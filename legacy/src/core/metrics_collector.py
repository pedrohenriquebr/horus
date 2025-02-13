"""Metrics collector for the Horus bot system."""
import time
import sqlite3
from datetime import datetime
import threading
from typing import Dict, List, Optional
import logging
import os
import json
from dotenv import load_dotenv

logger = logging.getLogger(__name__)

class MetricsCollector:
    def __init__(self, db_path: str = None):
        load_dotenv()
        if db_path is None:
            # Use absolute path in the project root
            project_root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
            db_path = os.path.join(project_root, "metrics.db")
        self.db_path = db_path
        self._setup_database()
        self.lock = threading.Lock()
        
    def _setup_database(self):
        """Initialize SQLite database with required tables."""
        conn = sqlite3.connect(self.db_path)
        c = conn.cursor()
        
        # Message processing metrics
        c.execute('''CREATE TABLE IF NOT EXISTS message_metrics (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
            message_type TEXT,
            processing_time FLOAT,
            success BOOLEAN,
            error_message TEXT
        )''')
        
        # Memory operation metrics
        c.execute('''CREATE TABLE IF NOT EXISTS memory_metrics (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
            operation_type TEXT,
            success BOOLEAN,
            latency FLOAT,
            cache_hit BOOLEAN,
            embedding_time FLOAT
        )''')
        
        # Resource usage metrics
        c.execute('''CREATE TABLE IF NOT EXISTS resource_metrics (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
            resource_type TEXT,
            usage_value FLOAT,
            metric_name TEXT
        )''')
        
        # Bot status changes
        c.execute('''CREATE TABLE IF NOT EXISTS bot_status (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
            status TEXT,
            reason TEXT
        )''')
        
        # Request/response log
        c.execute('''CREATE TABLE IF NOT EXISTS request_response_log (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
            user_id TEXT,
            request_text TEXT,
            response_text TEXT,
            processing_time FLOAT,
            model_used TEXT,
            tokens_used INTEGER,
            cache_hit BOOLEAN,
            used_memories TEXT,
            working_memories TEXT,
            chat_history TEXT
        )''')
        
        conn.commit()
        conn.close()
    
    def record_message_metric(self, message_type: str, processing_time: float, 
                            success: bool, error_message: Optional[str] = None):
        """Record metrics about message processing."""
        with sqlite3.connect(self.db_path) as conn:
            c = conn.cursor()
            c.execute('''INSERT INTO message_metrics 
                        (message_type, processing_time, success, error_message)
                        VALUES (?, ?, ?, ?)''',
                     (message_type, processing_time, success, error_message))
    
    def record_memory_metric(self, operation_type: str, success: bool,
                           latency: float, cache_hit: bool, embedding_time: float = 0.0):
        """Record metrics about memory operations."""
        with sqlite3.connect(self.db_path) as conn:
            c = conn.cursor()
            c.execute('''INSERT INTO memory_metrics 
                        (operation_type, success, latency, cache_hit, embedding_time)
                        VALUES (?, ?, ?, ?, ?)''',
                     (operation_type, success, latency, cache_hit, embedding_time))
    
    def record_resource_metric(self, resource_type: str, usage_value: float, metric_name: str):
        """Record metrics about resource usage."""
        with sqlite3.connect(self.db_path) as conn:
            c = conn.cursor()
            c.execute('''INSERT INTO resource_metrics 
                        (resource_type, usage_value, metric_name)
                        VALUES (?, ?, ?)''',
                     (resource_type, usage_value, metric_name))
    
    def record_bot_status(self, status: str, reason: Optional[str] = None):
        """Record bot status changes."""
        with sqlite3.connect(self.db_path) as conn:
            c = conn.cursor()
            c.execute('''INSERT INTO bot_status (status, reason)
                        VALUES (?, ?)''',
                     (status, reason))
    
    def record_interaction(self, user_id: str, request_text: str, response_text: str,
                         processing_time: float, model_used: str, tokens_used: int,
                         cache_hit: bool, used_memories: List[str] = None,
                         working_memories: List[str] = None, chat_history: str = None):
        """Record request/response interaction with memory context."""
        with sqlite3.connect(self.db_path) as conn:
            c = conn.cursor()
            c.execute('''INSERT INTO request_response_log 
                        (user_id, request_text, response_text, processing_time,
                         model_used, tokens_used, cache_hit, used_memories,
                         working_memories, chat_history)
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)''',
                     (user_id, request_text, response_text, processing_time,
                      model_used, tokens_used, cache_hit,
                      json.dumps(used_memories) if used_memories else None,
                      json.dumps(working_memories) if working_memories else None,
                      chat_history))
    
    def get_recent_metrics(self, table: str, hours: int = 24) -> List[Dict]:
        """Get metrics from the last N hours."""
        with sqlite3.connect(self.db_path) as conn:
            conn.row_factory = sqlite3.Row
            c = conn.cursor()
            c.execute(f'''SELECT * FROM {table}
                         WHERE timestamp >= datetime('now', '-{hours} hours')
                         ORDER BY timestamp DESC''')
            return [dict(row) for row in c.fetchall()]
    
    def get_current_bot_status(self) -> Dict:
        """Get the most recent bot status."""
        with sqlite3.connect(self.db_path) as conn:
            conn.row_factory = sqlite3.Row
            c = conn.cursor()
            c.execute('''SELECT * FROM bot_status
                        ORDER BY timestamp DESC
                        LIMIT 1''')
            result = c.fetchone()
            return dict(result) if result else {"status": "unknown", "timestamp": None}
    
    def get_performance_metrics(self, hours: int = 24) -> Dict:
        """Get aggregated performance metrics."""
        with sqlite3.connect(self.db_path) as conn:
            c = conn.cursor()
            
            # Message success rate
            c.execute('''SELECT 
                            COUNT(*) as total,
                            SUM(CASE WHEN success = 1 THEN 1 ELSE 0 END) as successful
                        FROM message_metrics
                        WHERE timestamp >= datetime('now', '-? hours')''',
                     (hours,))
            msg_stats = c.fetchone()
            
            # Average processing times
            c.execute('''SELECT 
                            message_type,
                            AVG(processing_time) as avg_time,
                            COUNT(*) as count
                        FROM message_metrics
                        WHERE timestamp >= datetime('now', '-? hours')
                        GROUP BY message_type''',
                     (hours,))
            processing_times = {row[0]: {"avg_time": row[1], "count": row[2]} 
                              for row in c.fetchall()}
            
            # Memory operation stats
            c.execute('''SELECT 
                            operation_type,
                            COUNT(*) as total,
                            SUM(CASE WHEN success = 1 THEN 1 ELSE 0 END) as successful,
                            AVG(latency) as avg_latency,
                            SUM(CASE WHEN cache_hit = 1 THEN 1 ELSE 0 END) as cache_hits
                        FROM memory_metrics
                        WHERE timestamp >= datetime('now', '-? hours')
                        GROUP BY operation_type''',
                     (hours,))
            memory_stats = {row[0]: {
                "total": row[1],
                "success_rate": row[2]/row[1] if row[1] > 0 else 0,
                "avg_latency": row[3],
                "cache_hit_rate": row[4]/row[1] if row[1] > 0 else 0
            } for row in c.fetchall()}
            
            return {
                "message_stats": {
                    "total": msg_stats[0],
                    "success_rate": msg_stats[1]/msg_stats[0] if msg_stats[0] > 0 else 0
                },
                "processing_times": processing_times,
                "memory_stats": memory_stats
            }
    
    def get_user_interactions(self, user_id: str, limit: int = 100) -> List[Dict]:
        """Get recent interactions for a user."""
        with sqlite3.connect(self.db_path) as conn:
            c = conn.cursor()
            c.execute('''SELECT timestamp, request_text, response_text, processing_time,
                               model_used, tokens_used, cache_hit, used_memories,
                               working_memories, chat_history
                        FROM request_response_log
                        WHERE user_id = ?
                        ORDER BY timestamp DESC
                        LIMIT ?''', (user_id, limit))
            rows = c.fetchall()
            return [{
                'timestamp': row[0],
                'request': row[1],
                'response': row[2],
                'processing_time': row[3],
                'model': row[4],
                'tokens': row[5],
                'cache_hit': row[6],
                'used_memories': json.loads(row[7]) if row[7] else [],
                'working_memories': json.loads(row[8]) if row[8] else [],
                'chat_history': row[9]
            } for row in rows]
            
    def get_system_info(self) -> Dict:
        """Get system information."""
        # Default system info with basic capabilities
        info = {
            "model": "gemini-1.5-flash",  # Default model
            "max_history": 80,  # Default max history
            "max_working_memory": 30,  # Default max working memory
            "cleanup_interval": "2 minutes",  # Default cleanup interval
            "capabilities": {
                "text_processing": True,
                "image_processing": True,
                "audio_processing": True,
                "memory_management": True
            }
        }
        
        try:
            from core.llm_handler import LLMHandler
            llm = LLMHandler()
            
            # Update with actual values if available
            if hasattr(llm, 'model'):
                info['model'] = llm.model
            if hasattr(llm, 'max_history'):
                info['max_history'] = llm.max_history
            if hasattr(llm, 'max_working_memory'):
                info['max_working_memory'] = llm.max_working_memory
            if hasattr(llm, 'cleanup_interval'):
                info['cleanup_interval'] = str(llm.cleanup_interval)
                
        except Exception as e:
            logger.error(f"Error getting LLM info: {e}")
            # Continue with default values
            
        return info
    
    def get_active_context(self) -> List[Dict]:
        """Get current active context from Redis."""
        try:
            from core.redis_cache import RedisCache
            redis = RedisCache()
            context = redis.get_active_context()
            # Convert context to list of dicts with required fields
            formatted_context = []
            for item in context:
                if isinstance(item, dict):
                    formatted_context.append({
                        'timestamp': item.get('timestamp', datetime.now().strftime('%Y-%m-%d %H:%M:%S')),
                        'type': item.get('type', 'unknown'),
                        'content': item.get('content', str(item))
                    })
                else:
                    # If item is not a dict, create a basic context entry
                    formatted_context.append({
                        'timestamp': datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                        'type': 'unknown',
                        'content': str(item)
                    })
            return formatted_context
        except Exception as e:
            logger.error(f"Error getting active context: {e}")
            return []
    
    def clear_context(self):
        """Clear all context from Redis."""
        try:
            from core.redis_cache import RedisCache
            redis = RedisCache()
            keys = redis.redis.keys("horus:memory:*")
            if keys:
                redis.redis.delete(*keys)
        except Exception as e:
            logger.error(f"Error clearing context: {e}")
    
    def get_chat_history(self, user_id: str) -> List[Dict]:
        """Get chat history for a user."""
        try:
            from core.supabase_rag import SupabaseRAG
            rag = SupabaseRAG(self)
            print('mah oi')
            return rag.get_user_messages(user_id)
        except Exception as e:
            logger.error(f"Error getting chat history: {e}")
            return []
    
    def get_working_memory(self, user_id: str) -> List[Dict]:
        """Get working memory for a user."""
        try:
            from core.redis_cache import RedisCache
            redis = RedisCache()
            return redis.get_memories(user_id)
        except Exception as e:
            logger.error(f"Error getting working memory: {e}")
            return []
