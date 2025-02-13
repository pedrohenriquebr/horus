import re
from collections import defaultdict
from math import log

class SimpleRAG:
    def __init__(self):
        self.documents = []
        self.index = defaultdict(list)
        self.doc_lengths = {}
        
    def preprocess_text(self, text):
        """Simplifica o texto para indexação"""
        text = text.lower()
        text = re.sub(r'[^\w\s]', '', text)
        return text.split()
    
    def add_document(self, text, doc_id=None):
        """Adiciona um documento ao índice"""
        if doc_id is None:
            doc_id = len(self.documents)
        
        self.documents.append((doc_id, text))
        words = self.preprocess_text(text)
        
        # Conta frequência das palavras no documento
        word_freq = defaultdict(int)
        for word in words:
            word_freq[word] += 1
        
        # Atualiza o índice invertido
        for word, freq in word_freq.items():
            self.index[word].append((doc_id, freq))
        
        self.doc_lengths[doc_id] = len(words)
    
    def search(self, query, top_k=3):
        """Busca documentos similares usando BM25-like scoring"""
        query_words = self.preprocess_text(query)
        scores = defaultdict(float)
        
        # Parâmetros do BM25
        k1 = 1.5
        b = 0.75
        avg_doc_length = sum(self.doc_lengths.values()) / len(self.doc_lengths) if self.doc_lengths else 0
        
        for word in query_words:
            if word in self.index:
                idf = log((1 + len(self.documents)) / (1 + len(self.index[word])))
                
                for doc_id, freq in self.index[word]:
                    doc_length = self.doc_lengths[doc_id]
                    numerator = freq * (k1 + 1)
                    denominator = freq + k1 * (1 - b + b * doc_length / avg_doc_length)
                    scores[doc_id] += idf * numerator / denominator
        
        # Retorna os top_k documentos
        sorted_scores = sorted(scores.items(), key=lambda x: x[1], reverse=True)[:top_k]
        return [(self.documents[doc_id][1], score) for doc_id, score in sorted_scores]