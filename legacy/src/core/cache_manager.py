import sqlite3
import hashlib
import zlib
from datetime import datetime, timedelta

class CacheManager:
    def __init__(self):
        self.conn = sqlite3.connect('cache.db')
        self.init_db()
        
    def init_db(self):
        cursor = self.conn.cursor()
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS prompt_cache (
                input_hash TEXT PRIMARY KEY,
                prompt TEXT,
                response BLOB,
                timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        ''')
        self.conn.commit()
    
    def get_cache(self, prompt):
        cursor = self.conn.cursor()
        prompt_hash = hashlib.md5(prompt.encode()).hexdigest()
        cursor.execute('SELECT response FROM prompt_cache WHERE input_hash = ?', (prompt_hash,))
        result = cursor.fetchone()
        return result[0] if result else None
    
    def set_cache(self, prompt, response):
        cursor = self.conn.cursor()
        prompt_hash = hashlib.md5(prompt.encode()).hexdigest()
        cursor.execute('INSERT OR REPLACE INTO prompt_cache (input_hash, prompt, response) VALUES (?, ?, ?)',
                      (prompt_hash, prompt, response))
        self.conn.commit()