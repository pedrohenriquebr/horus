import ollama
import numpy as np
from sklearn.metrics.pairwise import cosine_distances
import hdbscan
import spacy
from typing import List

nlp = spacy.load("en_core_web_sm")

class SemanticChunkStrategy:
    def __init__(self, embedding_model: str, similarity_threshold: float = 0.5, 
                 allow_overlapping_clusters: bool = False, min_cluster_size: int = 2):
        self.embedding_model = embedding_model
        self.similarity_threshold = similarity_threshold
        self.allow_overlapping_clusters = allow_overlapping_clusters
        self.min_cluster_size = min_cluster_size  # Novo parâmetro

    def split_chunks(self, doc_id: str, content: str) -> List[dict]:
        sentences = self.split_into_sentences(content)
        if not sentences:
            return []
            
        embeddings = self.generate_embeddings(sentences)
        clusters = self.cluster_sentences(embeddings)
        return self.create_chunks(doc_id, clusters, sentences)

    def split_into_sentences(self, content: str) -> List[str]:
        doc = nlp(content)
        return [sent.text.strip() for sent in doc.sents if len(sent.text.strip()) > 0]

    def generate_embeddings(self, sentences: List[str]) -> np.ndarray:
        return np.array([
            np.array(ollama.embeddings(model=self.embedding_model, prompt=sentence)['embedding'])
            for sentence in sentences
        ])

    def cluster_sentences(self, embeddings: np.ndarray) -> List[List[int]]:
        if len(embeddings) < self.min_cluster_size:
            print('DBG: Not enough sentences to cluster')
            return [list(range(len(embeddings)))]  # Retorna único cluster se não houver pontos suficientes
            
        distance_matrix = cosine_distances(embeddings)
        
        clusterer = hdbscan.HDBSCAN(
            min_cluster_size=2,    # Reduza se necessário
            min_samples=1,         # Permite clusters menores
            cluster_selection_epsilon=0.1,  # Agrupa clusters próximos
            metric='precomputed',
            allow_single_cluster=True  # Permite um único cluster
        )
        clusterer.fit(distance_matrix)
        
        clusters = []
        print('DBG: Number of clusters:', len(np.unique(clusterer.labels_)))

        for cluster_id in np.unique(clusterer.labels_):
            print('DBG: Cluster ID:', cluster_id)
            if cluster_id != -1:
                cluster = np.where(clusterer.labels_ == cluster_id)[0].tolist()
                clusters.append(cluster)
                
        # Caso nenhum cluster seja encontrado, agrupa tudo
        if not clusters and len(embeddings) > 0:
            clusters = [list(range(len(embeddings)))]
            
        return clusters

    def create_chunks(self, doc_id: str, clusters: List[List[int]], sentences: List[str]) -> List[dict]:
        chunks = []
        for i, cluster in enumerate(clusters):
            chunk_text = " ".join(sentences[i] for i in cluster)
            chunks.append({
                "doc_id": doc_id,
                "chunk_number": i+1,
                "chunk_text": chunk_text,
                "token_count": len(chunk_text.split()),
                "cluster_size": len(cluster)
            })
        return chunks

# Uso corrigido
if __name__ == "__main__":
    strategy = SemanticChunkStrategy(
        embedding_model="all-minilm:l6-v2",
        similarity_threshold=0.3,  # Valor mais baixo
        min_cluster_size=5  # Cluster mínimo de 2 sentenças
    )
    
    text = """
Artificial intelligence is transforming industries. 
Machine learning enables pattern recognition. 
Deep learning uses neural networks. 
NLP processes human language. 
Robotics combines hardware and AI.
"""
    chunks = strategy.split_chunks("doc1", text)
    
    print(f"Generated {len(chunks)} chunks:")
    for chunk in chunks:
        print(f"\nChunk {chunk['chunk_number']} ({chunk['cluster_size']} sentences):")
        print(chunk['chunk_text'])