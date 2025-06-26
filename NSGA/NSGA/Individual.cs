using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSGAI
{
    public partial class Individual
    {
        public List<int> Allocation { get; set; } = new List<int>();
        public float Cost { get; set; }
        public float Error { get; set; }
        public int DominatedCount { get; set; }
        public List<Individual> DominatedIndividuals { get; set; } = new List<Individual>();
        public int Rank { get; set; }
        public float CrowdingDistance { get; set; }

        public void CalculateCostAndError(List<Worker> workers)
        {
            Cost = 0;
            Error = 0;

            for (int i = 0; i < Allocation.Count; i++)
            {
                Cost += Allocation[i] * workers[i].HourlyWage;
                Error += Allocation[i] * workers[i].ErrorRate;
            }
        }
    }
}
