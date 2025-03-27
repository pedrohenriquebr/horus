using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;

public class KMeans
{
    private readonly float[][] _embeddings;
    private readonly int _k;
    private readonly int _maxIterations;

    public KMeans(float[][] embeddings, int k, int maxIterations = 100)
    {
        _embeddings = embeddings;
        _k = k;
        _maxIterations = maxIterations;
        
    }

    public List<List<int>> Cluster()
    {
        // Converta float[][] para double[][] antes de criar a matriz
        var doubleEmbeddings = _embeddings.Select(row => 
            row.Select(f => (double)f).ToArray()
        ).ToArray();
        
        var data = DenseMatrix.OfRows(doubleEmbeddings.Length, doubleEmbeddings[0].Length, doubleEmbeddings);
        var random = new Random();
        var centroids = InitializeCentroids(data, random);

        List<int> assignments = new List<int>();
        for (int iteration = 0; iteration < _maxIterations; iteration++)
        {
            assignments = AssignClusters(data, centroids);
            var newCentroids = UpdateCentroids(data, assignments);
            if (CentroidsConverged(centroids, newCentroids))
                break;
            centroids = newCentroids;
        }

        return AssignmentsToClusters(assignments);
    }

    private List<Vector<double>> InitializeCentroids(Matrix<double> data, Random random)
    {
        var centroids = new List<Vector<double>>();
        var indices = Enumerable.Range(0, data.RowCount).OrderBy(x => random.Next()).Take(_k).ToList();
        foreach (var index in indices)
        {
            centroids.Add(DenseVector.OfArray(data.Row(index).ToArray()));
        }
        return centroids;
    }

    private List<int> AssignClusters(Matrix<double> data, List<Vector<double>> centroids)
    {
        var assignments = new List<int>();
        foreach (var row in data.EnumerateRows())
        {
            var distances = centroids.Select(c => EuclideanDistance(row, c)).ToArray();
            var assignedCluster = Array.IndexOf(distances, distances.Min());
            assignments.Add(assignedCluster);
        }
        return assignments;
    }

    private List<Vector<double>> UpdateCentroids(Matrix<double> data, List<int> assignments)
    {
        var newCentroids = new List<Vector<double>>(_k);
        var random = new Random();
        for (int i = 0; i < _k; i++)
        {
            var assignedRows = data.EnumerateRows().Where((row, index) => assignments[index] == i);
            if (assignedRows.Any())
            {
                var meanRow = assignedRows.Aggregate((sum, row) => sum + row) / assignedRows.Count();
                newCentroids.Add(DenseVector.OfArray( meanRow.ToArray()));
            }
            else
            {
                newCentroids.Add(DenseVector.OfArray(data.Row(random.Next(data.RowCount)).ToArray()));
            }
        }
        return newCentroids;
    }

    private bool CentroidsConverged(List<Vector<double>> oldCentroids, List<Vector<double>> newCentroids)
    {
        for (int i = 0; i < _k; i++)
        {
            if (EuclideanDistance(oldCentroids[i], newCentroids[i]) > 1e-4)
                return false;
        }
        return true;
    }

    private double EuclideanDistance(Vector<double> v1, Vector<double> v2)
    {
        return (v1 - v2).L2Norm();
    }

    private List<List<int>> AssignmentsToClusters(List<int> assignments)
    {
        var clusters = new List<List<int>>(_k);
        for (int i = 0; i < _k; i++)
        {
            clusters.Add(new List<int>());
        }
        for (int i = 0; i < assignments.Count; i++)
        {
            clusters[assignments[i]].Add(i);
        }
        return clusters;
    }
}
