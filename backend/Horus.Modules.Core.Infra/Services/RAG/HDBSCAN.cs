using HdbscanSharp.Runner;
using System;
using System.Collections.Generic;
using System.Linq;
using HdbscanSharp.Distance;

public sealed class HDBSCAN
{
    private readonly double[][] _embeddings;
    private readonly int _minPts;
    private readonly int _minClusterSize;

    public HDBSCAN(float[][] embeddings, int minPts, int minClusterSize)
    {
        // Converter float para double conforme exigido pela biblioteca [[2]]
        _embeddings = embeddings.Select(row => 
            row.Select(f => (double)f).ToArray()
        ).ToArray();
        
        _minPts = minPts;
        _minClusterSize = minClusterSize;
    }

    public List<List<int>> Cluster()
    {
        // Executar HDBSCAN [[2]]
        var result = HdbscanRunner.Run(
            datasetCount: _embeddings.Length,
            minPoints: _minPts,
            minClusterSize: _minClusterSize,
            distanceFunc: GenericCosineSimilarity.GetFunc(_embeddings)
        );

        // Converter labels para clusters [[2]]
        return ProcessLabels(result.Labels);
    }

    private List<List<int>> ProcessLabels(int[] labels)
    {
        var clusters = new Dictionary<int, List<int>>();
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] == -1) continue; // Ignorar ruído
            
            if (!clusters.ContainsKey(labels[i]))
                clusters[labels[i]] = new List<int>();
            
            clusters[labels[i]].Add(i);
        }
        return clusters.Values.ToList();
    }
}