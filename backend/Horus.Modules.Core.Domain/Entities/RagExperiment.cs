using Horus.Modules.Core.Domain.Entities; // If you have a base entity or domain folder
using System;
using System.Collections.Generic;

namespace Horus.Modules.Core.Domain.Entities
{
    public class RagExperiment 
    {
        public int Id { get; set; }
        
        /// <summary>
        /// A descriptive name for the experiment. 
        /// For example: "Exp_2025-03-23_Threshold_0_7"
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The date/time the experiment was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Stores the JSON representation of the RAG options used for the experiment
        /// (SearchLimit, SearchThreshold, chunking strategies, etc.).
        /// </summary>
        public string RagOptionsJson { get; set; }

        /// <summary>
        /// The final average precision for the experiment.
        /// </summary>
        public double AveragePrecision { get; set; }

        /// <summary>
        /// The final average recall for the experiment.
        /// </summary>
        public double AverageRecall { get; set; }

        /// <summary>
        /// The final average F1 score for the experiment.
        /// </summary>
        public double AverageF1 { get; set; }
    }
}