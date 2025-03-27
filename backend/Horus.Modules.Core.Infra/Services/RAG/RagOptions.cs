namespace Horus.Modules.Core.Infra.Services.RAG;

public class RagOptions
{ 
    public float SearchThresold { get; set; }
    public int SearchLimit { get; set; }
    public string ChunkStrategy { get; set; }
    public ChunkStrategiesOptions ChunkStrategies { get; set; }
}


public class ChunkStrategiesOptions
{
    public FixedChunkOptions FixedChunk { get; set; }
    public SemanticChunkOptions SemanticChunk { get; set; }
}

public class SemanticChunkOptions
{
    // Define o modelo de embedding usado para análise semântica
    public string EmbeddingModel { get; set; } = "all-MiniLM-L6-v2"; 
    
    // Limiar de similaridade para agrupamento de chunks (ex: 0.75)
    public float SimilarityThreshold { get; set; } = 0.75f; 
    
    // Tamanho mínimo/máximo de clusters semânticos
    public int MinClusterSize { get; set; } = 3; 
    public int MaxClusterSize { get; set; } = 10; 
    
    // Define se permite sobreposição entre clusters
    public bool AllowOverlappingClusters { get; set; } = false; 
    
    // Dimensão do embedding gerado pelo modelo
    public int EmbeddingDimension { get; set; } = 384; 
    
    // Algoritmo de agrupamento (ex: HDBSCAN, K-means)
    public string ClusteringAlgorithm { get; set; } = "HDBSCAN";
}

public class FixedChunkOptions
{
    public int ChunkSize { get; set; }
    public int Overlap { get; set; }
}