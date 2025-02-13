"""FastAPI backend for the Horus bot dashboard."""
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import Dict, List, Optional
import sys
import os
from pathlib import Path
import time
import logging
import sqlite3
from datetime import datetime, timedelta

# Add parent directory to path to import core modules
sys.path.append(str(Path(__file__).parent.parent))
from dotenv import load_dotenv
load_dotenv()   

print(os.getenv("SUPABASE_URL"))
print(os.getenv('REDIS_HOST', 'localhost'))
print(os.getenv('REDIS_PORT', '6379'))
print(os.getenv('REDIS_PASSWORD', ''))

from core.metrics_collector import MetricsCollector
from core.llm_handler import LLMHandler
from core.redis_cache import RedisCache
from core.supabase_rag import SupabaseRAG

app = FastAPI(title="Horus Bot Dashboard API")

# Configure CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Initialize components
metrics = MetricsCollector()

# Get database path from metrics collector
DB_PATH = metrics.db_path

# Initialize SQLite database
def init_db():
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    
    # Create metrics table if it doesn't exist
    cursor.execute('''
    CREATE TABLE IF NOT EXISTS metrics (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        timestamp TEXT NOT NULL,
        metric_type TEXT NOT NULL,
        value TEXT,
        success INTEGER DEFAULT 1,
        duration REAL,
        user_id TEXT
    )
    ''')
    
    conn.commit()
    conn.close()

init_db()

# Initialize bot status if empty
try:
    current_status = metrics.get_current_bot_status()
    if current_status["status"] == "unknown":
        metrics.record_bot_status("initialized", "Initial bot status")
except Exception:
    metrics.record_bot_status("initialized", "Initial bot status")

# Bot process management
import subprocess
import signal
import psutil

class BotProcess:
    _instance = None
    
    def __init__(self):
        self.process = None
        self.log_file = None
        self.status = "stopped"
        
    @classmethod
    def get_instance(cls):
        if cls._instance is None:
            cls._instance = BotProcess()
        return cls._instance
    
    def start(self):
        if self.process and self.process.poll() is None:
            return {"status": "already_running", "message": "Bot is already running"}
            
        try:
            # Abre arquivo de log em modo append
            self.log_file = open('/home/pedrobr/Documents/repos/horus/bot.log', 'a')
            
            # Inicia o processo do bot
            self.process = subprocess.Popen(
                ["python", "src/main.py"],
                cwd="/home/pedrobr/Documents/repos/horus",
                stdout=subprocess.DEVNULL,
                stderr=self.log_file,
                bufsize=1,  # Line buffered
                universal_newlines=True  # Text mode
            )
            
            # Aguarda 2 segundos para verificar se o processo iniciou corretamente
            import time
            time.sleep(2)
            
            if self.process.poll() is not None:
                # Processo morreu
                error_msg = "Bot process failed to start"
                self.status = "stopped"
                return {"status": "error", "message": error_msg}
            
            self.status = "running"
            return {"status": "success", "message": "Bot started successfully"}
            
        except Exception as e:
            self.status = "stopped"
            if self.log_file:
                self.log_file.close()
            return {"status": "error", "message": str(e)}
    
    def stop(self):
        if not self.process:
            return {"status": "not_running", "message": "Bot is not running"}
            
        try:
            self.process.terminate()
            try:
                self.process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                self.process.kill()
            
            if self.log_file:
                self.log_file.close()
                self.log_file = None
                
            self.status = "stopped"
            return {"status": "success", "message": "Bot stopped successfully"}
        except Exception as e:
            return {"status": "error", "message": str(e)}
    
    def pause(self):
        if not self.process or self.process.poll() is not None:
            return {"status": "not_running", "message": "Bot is not running"}
            
        try:
            self.process.send_signal(signal.SIGSTOP)
            self.status = "paused"
            return {"status": "success", "message": "Bot paused successfully"}
        except Exception as e:
            return {"status": "error", "message": str(e)}
    
    def resume(self):
        if not self.process or self.process.poll() is not None:
            return {"status": "not_running", "message": "Bot is not running"}
            
        try:
            self.process.send_signal(signal.SIGCONT)
            self.status = "running"
            return {"status": "success", "message": "Bot resumed successfully"}
        except Exception as e:
            return {"status": "error", "message": str(e)}

bot_process = BotProcess.get_instance()

class BotCommand(BaseModel):
    command: str
    reason: Optional[str] = None

@app.post("/control")
async def control_bot(command: BotCommand):
    """Control bot operation (start/stop/pause)."""
    try:
        success = False
        message = ""
        
        if command.command.lower() == "start":
            result = bot_process.start()
            if result["status"] == "success":
                success = True
                metrics.record_bot_status("running", command.reason or "Bot started")
            else:
                message = result["message"]
        elif command.command.lower() == "stop":
            result = bot_process.stop()
            if result["status"] == "success":
                success = True
                metrics.record_bot_status("stopped", command.reason or "Bot stopped")
            else:
                message = result["message"]
        elif command.command.lower() == "pause":
            result = bot_process.pause()
            if result["status"] == "success":
                success = True
                metrics.record_bot_status("paused", command.reason or "Bot paused")
            else:
                message = result["message"]
        elif command.command.lower() == "resume":
            result = bot_process.resume()
            if result["status"] == "success":
                success = True
                metrics.record_bot_status("running", command.reason or "Bot resumed")
            else:
                message = result["message"]
        
        return {
            "success": success, 
            "message": message
        }
    except Exception as e:
        logging.error(f"Erro no controle do bot: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/status")
async def get_status():
    """Get current bot status and basic metrics."""
    try:
        status = metrics.get_current_bot_status()
        if status is None:
            status = {"status": "unknown", "timestamp": None}
        
        try:
            perf_metrics = metrics.get_performance_metrics(hours=24)
        except Exception:
            perf_metrics = {
                "message_stats": {"total": 0, "success_rate": 0},
                "processing_times": {},
                "memory_stats": {}
            }
            
        return {
            "status": status,
            "metrics": perf_metrics
        }
    except Exception as e:
        return {
            "status": {"status": "error", "timestamp": None},
            "metrics": {
                "message_stats": {"total": 0, "success_rate": 0},
                "processing_times": {},
                "memory_stats": {}
            }
        }

@app.get("/metrics/messages")
async def get_message_metrics(hours: int = 24):
    """Get detailed message processing metrics."""
    try:
        return metrics.get_recent_metrics("message_metrics", hours)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/metrics/memory")
async def get_memory_metrics(hours: int = 24):
    """Get detailed memory operation metrics."""
    try:
        conn = sqlite3.connect(DB_PATH)
        cursor = conn.cursor()
        
        cutoff = datetime.now() - timedelta(hours=hours)
        query = """
        SELECT 
            strftime('%Y-%m-%d %H:%M', timestamp) as time_bucket,
            COUNT(*) as total_operations,
            AVG(latency) as avg_latency,
            SUM(CASE WHEN cache_hit = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) as cache_hit_rate,
            AVG(embedding_time) as avg_embedding_time
        FROM memory_metrics
        WHERE timestamp >= ?
        GROUP BY time_bucket
        ORDER BY time_bucket DESC
        """
        
        cursor.execute(query, (cutoff.strftime('%Y-%m-%d %H:%M:%S'),))
        columns = [col[0] for col in cursor.description]
        results = [dict(zip(columns, row)) for row in cursor.fetchall()]
        
        return results
    except Exception as e:
        logging.error(f"Error getting memory metrics: {e}")
        return []

@app.get("/metrics/api")
async def get_api_metrics():
    """Get API metrics."""
    try:
        conn = sqlite3.connect(DB_PATH)
        cursor = conn.cursor()
        
        query = """
        SELECT 
            strftime('%Y-%m-%d %H:%M', timestamp) as time_bucket,
            COUNT(*) as total_requests,
            AVG(CASE WHEN success = 1 THEN processing_time ELSE NULL END) as avg_response_time,
            SUM(CASE WHEN success = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) as success_rate
        FROM message_metrics
        GROUP BY time_bucket
        ORDER BY time_bucket DESC
        LIMIT 100
        """
        
        cursor.execute(query)
        columns = [col[0] for col in cursor.description]
        results = [dict(zip(columns, row)) for row in cursor.fetchall()]
        
        return results
    except Exception as e:
        logging.error(f"Error getting API metrics: {e}")
        return []

@app.get("/metrics/cache")
async def get_cache_metrics():
    """Get cache metrics."""
    try:
        conn = sqlite3.connect(DB_PATH)
        cursor = conn.cursor()
        
        query = """
        SELECT 
            strftime('%Y-%m-%d %H:%M', timestamp) as time_bucket,
            SUM(CASE WHEN cache_hit = 1 THEN 1 ELSE 0 END) as cache_hits,
            SUM(CASE WHEN cache_hit = 0 THEN 1 ELSE 0 END) as cache_misses,
            AVG(latency) as avg_latency
        FROM memory_metrics
        GROUP BY time_bucket
        ORDER BY time_bucket DESC
        LIMIT 100
        """
        
        cursor.execute(query)
        columns = [col[0] for col in cursor.description]
        results = [dict(zip(columns, row)) for row in cursor.fetchall()]
        
        return results
    except Exception as e:
        logging.error(f"Error getting cache metrics: {e}")
        return []

@app.get("/metrics/resources")
async def get_resource_metrics(hours: int = 24):
    """Get detailed resource usage metrics."""
    try:
        return metrics.get_recent_metrics("resource_metrics", hours)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/bot/performance")
async def get_performance_metrics(hours: int = 24):
    """Get aggregated performance metrics."""
    try:
        return metrics.get_performance_metrics(hours)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

# Novos endpoints

@app.get("/users/active")
async def get_active_users():
    """Get list of active users."""
    try:
        # Busca usuários ativos das últimas 24 horas
        cutoff = datetime.now() - timedelta(hours=24)
        conn = sqlite3.connect(DB_PATH)
        cursor = conn.cursor()
        
        query = """
        SELECT DISTINCT user_id, MAX(timestamp) as last_activity
        FROM metrics
        WHERE timestamp > ? AND user_id IS NOT NULL
        GROUP BY user_id
        """
        
        cursor.execute(query, (cutoff.isoformat(),))
        results = cursor.fetchall()
        
        active_users = []
        for user_id, last_activity in results:
            # Busca detalhes do usuário do Supabase
            user_details = supabase_rag.get_user_details(user_id)
            if user_details:
                active_users.append({
                    'user_id': user_id,
                    'name': user_details.get('name', 'Unknown'),
                    'last_activity': last_activity,
                    'status': 'active'
                })
        
        return active_users
    except Exception as e:
        logging.error(f"Error getting active users: {e}")
        return []

@app.get("/users/{user_id}")
async def get_user_details(user_id: str):
    """Get detailed information about a specific user."""
    try:
        # Busca detalhes do usuário do Supabase
        user_details = supabase_rag.get_user_details(user_id)
        if not user_details:
            raise HTTPException(status_code=404, detail="User not found")
            
        # Adiciona métricas do usuário
        conn = sqlite3.connect(DB_PATH)
        cursor = conn.cursor()
        
        # Última atividade
        cursor.execute("""
            SELECT MAX(timestamp) FROM metrics WHERE user_id = ?
        """, (user_id,))
        last_activity = cursor.fetchone()[0]
        
        # Total de mensagens
        cursor.execute("""
            SELECT COUNT(*) FROM metrics 
            WHERE user_id = ? AND metric_type = 'message'
        """, (user_id,))
        total_messages = cursor.fetchone()[0]
        
        return {
            **user_details,
            'last_activity': last_activity,
            'total_messages': total_messages
        }
    except Exception as e:
        logging.error(f"Error getting user details: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/users/{user_id}/interactions")
async def get_user_interactions(user_id: str):
    """Get interaction history for a specific user."""
    try:
        conn = sqlite3.connect(DB_PATH)
        cursor = conn.cursor()
        
        query = """
        SELECT timestamp,
               metric_type as interaction_type,
               value,
               success
        FROM metrics
        WHERE user_id = ?
        ORDER BY timestamp DESC
        LIMIT 100
        """
        
        cursor.execute(query, (user_id,))
        columns = [col[0] for col in cursor.description]
        results = [dict(zip(columns, row)) for row in cursor.fetchall()]
        
        return results
    except Exception as e:
        logging.error(f"Error getting user interactions: {e}")
        return []

@app.get("/context")
async def get_context():
    """Get current context."""
    try:
        # Get context through metrics instead of directly from redis
        return metrics.get_active_context()
    except Exception as e:
        logging.error(f"Error getting active context: {str(e)}")
        return {"error": str(e)}

@app.post("/context/clear")
async def clear_context():
    """Clear all context from Redis."""
    try:
        # Clear context through metrics instead of directly from redis
        metrics.clear_context()
        return {"status": "success", "message": "Context cleared successfully"}
    except Exception as e:
        logging.error(f"Error clearing context: {str(e)}")
        return {"error": str(e)}

@app.get("/log/operations")
async def get_operations_log():
    """Get recent operations log."""
    try:
        conn = sqlite3.connect(DB_PATH)
        cursor = conn.cursor()
        
        query = """
        SELECT 
            timestamp,
            message_type as operation,
            CASE WHEN success = 1 THEN 'success' ELSE 'error' END as status,
            error_message as details
        FROM message_metrics
        ORDER BY timestamp DESC
        LIMIT 100
        """
        
        cursor.execute(query)
        columns = [col[0] for col in cursor.description]
        results = [dict(zip(columns, row)) for row in cursor.fetchall()]
        
        return results
    except Exception as e:
        logging.error(f"Error getting operations log: {e}")
        return []

# Novos endpoints para memória e processamento

@app.get("/memory/working/{user_id}")
async def get_working_memory(user_id: str):
    """Get working memory for a specific user."""
    try:
        memories = metrics.get_working_memory(user_id)
        return {"memories": memories}
    except Exception as e:
        logger.error(f"Error getting working memory: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/memory/long_term/{user_id}")
async def get_long_term_memory(user_id: str):
    """Get long-term memories for a specific user from Supabase."""
    try:
        rag = SupabaseRAG(metrics)
        memories = rag.get_user_memories(user_id)
        return {"memories": memories}
    except Exception as e:
        logger.error(f"Error getting long-term memory: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/memory/similar")
async def get_similar_memories(query: str, user_id: str, limit: int = 10):
    """Get memories similar to a query."""
    try:
        rag = SupabaseRAG(metrics)
        memories = rag.search_similar(query, limit=limit)
        # Filtra apenas memórias do usuário
        relevant_memories = [
            mem for mem in memories 
            if mem.get('metadata', {}).get('type') == 'memory' 
            and str(mem.get('metadata', {}).get('user_id')) == str(user_id)
        ]
        return {"memories": relevant_memories}
    except Exception as e:
        logger.error(f"Error getting similar memories: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/chat/history/{user_id}")
async def get_user_chat_history(user_id: str, limit: int = 80):
    """Get chat history for a specific user."""
    try:
        rag = SupabaseRAG(metrics)
        messages = rag.get_user_messages(user_id, limit)
        return {"messages": messages}
    except Exception as e:
        logger.error(f"Error getting chat history: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/system/info")
async def get_system_info():
    """Get system information."""
    try:
        # Get system info through metrics collector instead of LLMHandler
        info = metrics.get_system_info()
        if not info:
            info = {
                "model": "gpt-4",  # Default values if not available
                "max_history": 50,
                "max_working_memory": 20,
                "cleanup_interval": "1h",
                "capabilities": {
                    "text_processing": True,
                    "image_processing": True,
                    "audio_processing": True,
                    "memory_management": True
                }
            }
        return info
    except Exception as e:
        logging.error(f"Error getting system info: {str(e)}")
        return {"error": str(e)}

@app.get("/metrics/memory_usage")
async def get_memory_usage_metrics(hours: int = 24):
    """Get memory usage metrics."""
    try:
        conn = sqlite3.connect(DB_PATH)
        cursor = conn.cursor()
        
        # Get metrics for the last X hours
        since = datetime.now() - timedelta(hours=hours)
        
        # Query for memory operations
        cursor.execute("""
            SELECT 
                strftime('%Y-%m-%d %H:00:00', timestamp) as hour,
                metric_type,
                COUNT(*) as total_ops,
                AVG(CASE WHEN success = 1 THEN duration ELSE NULL END) as avg_latency,
                SUM(CASE WHEN success = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) as success_rate
            FROM metrics 
            WHERE timestamp >= ? 
            AND metric_type IN ('text_storage', 'text_retrieval', 'memory_update')
            GROUP BY hour, metric_type
            ORDER BY hour DESC
        """, (since.strftime('%Y-%m-%d %H:%M:%S'),))
        
        results = cursor.fetchall()
        conn.close()
        
        # Format results
        metrics = []
        for row in results:
            metrics.append({
                "timestamp": row[0],
                "metric_type": row[1],
                "total_operations": row[2],
                "average_latency": row[3],
                "success_rate": row[4]
            })
        
        return {"metrics": metrics}
    except Exception as e:
        logger.error(f"Error getting memory usage metrics: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/metrics/context/{user_id}")
async def get_context_metrics(user_id: str):
    """Get detailed context metrics for a user."""
    try:
        # Get metrics through metrics collector instead of LLMHandler
        chat_history = metrics.get_chat_history(user_id)
        working_memories = metrics.get_working_memory(user_id)
        memory_metrics = metrics.get_recent_metrics("memory_metrics", 24)
        
        # Calculate performance metrics
        total_operations = sum(1 for m in memory_metrics if m['operation_type'] == 'text_processing')
        cache_hits = sum(1 for m in memory_metrics if m['cache_hit'])
        avg_latency = sum(m['latency'] for m in memory_metrics) / len(memory_metrics) if memory_metrics else 0
        
        return {
            'chat_history': chat_history,
            'working_memories': working_memories,
            'performance': {
                'total_operations': total_operations,
                'cache_hits': cache_hits,
                'cache_hit_ratio': cache_hits / total_operations if total_operations > 0 else 0,
                'avg_latency': avg_latency
            }
        }
    except Exception as e:
        logging.error(f"Error getting context metrics: {str(e)}")
        return {"error": str(e)}

@app.get("/metrics/interactions/{user_id}")
async def get_user_interactions(user_id: str):
    """Get all interactions for a user"""
    metrics = MetricsCollector()
    return metrics.get_user_interactions(user_id)

@app.get("/metrics/interactions/{user_id}/chat_history")
async def get_interaction_chat_history(user_id: str, timestamp: str):
    """Get chat history for a specific interaction"""
    metrics = MetricsCollector()
    try:
        # Busca a interação específica pelo timestamp
        with sqlite3.connect(metrics.db_path) as conn:
            c = conn.cursor()
            c.execute('''SELECT chat_history
                        FROM request_response_log
                        WHERE user_id = ? AND timestamp = ?''',
                     (user_id, timestamp))
            row = c.fetchone()
            if row and row[0]:
                return {"chat_history": row[0]}
            return {"chat_history": "Nenhum histórico encontrado"}
    except Exception as e:
        logging.error(f"Error getting chat history: {e}")
        raise HTTPException(status_code=500, detail=str(e))
