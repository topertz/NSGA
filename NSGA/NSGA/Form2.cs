using NSGAI;
using System.Text;

namespace NSGAII
{
    public partial class Form2 : Form
    {
        private Random rand = new Random();
        private List<Worker> workers = new List<Worker>();
        private int numWorkPieces;
        private int populationSize = 100;
        private int generations = 500;

        public Form2()
        {
            InitializeComponent();
            buttonLoad.Click += btnLoadFile_Click;
            btnRunAlgorithm.Click += btnRunAlgorithm_Click;
            btnRunAlgorithm.Enabled = false;
        }

        private void btnLoadFile_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog.Title = "Select Worker Data File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    LoadWorkers(openFileDialog.FileName);
                }
            }
        }

        private async void LoadWorkers(string filePath)
        {
            try
            {
                workers = await LoadWorkersFromFileAsync(filePath);
                if (workers.Count > 0)
                {
                    btnRunAlgorithm.Enabled = true;
                    MessageBox.Show("Worker data loaded successfully!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading worker data: " + ex.Message);
            }
        }

        private async Task<List<Worker>> LoadWorkersFromFileAsync(string filePath)
        {
            List<Worker> workers = new List<Worker>();

            try
            {
                string[] lines = await File.ReadAllLinesAsync(filePath);
                if (lines.Length < 2)
                    throw new Exception("File is empty or missing required data.");

                if (!int.TryParse(lines[0].Trim(), out int N))
                    throw new Exception("The first line should contain the number of workers.");

                numWorkPieces = 100;

                if (lines.Length < N + 1)
                    throw new Exception("The file does not contain enough lines for the specified number of workers.");

                for (int i = 1; i <= N; i++)
                {
                    string[] data = lines[i].Trim().Split(',');
                    if (data.Length != 2 || !float.TryParse(data[0], out float hourlyWage) || !float.TryParse(data[1], out float errorRate))
                        throw new Exception($"Line {i + 1} contains invalid data.");

                    workers.Add(new Worker
                    {
                        Id = i - 1,
                        HourlyWage = hourlyWage,
                        ErrorRate = errorRate
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading worker data: " + ex.Message);
            }

            return workers;
        }

        private async void btnRunAlgorithm_Click(object? sender, EventArgs e)
        {
            btnRunAlgorithm.Enabled = false;

            try
            {
                await Task.Run(() => RunNSGAII());
                MessageBox.Show("Algorithm completed!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error running algorithm: " + ex.Message);
            }
            finally
            {
                btnRunAlgorithm.Enabled = true;
            }
        }

        private void AssignParetoRanks(List<Individual> population)
        {
            foreach (Individual ind in population)
            {
                ind.Rank = 0;
                foreach (Individual other in population)
                {
                    if (Dominates(other, ind))
                    {
                        ind.Rank++;
                    }
                }
            }
        }

        private bool Dominates(Individual a, Individual b)
        {
            return (a.Cost <= b.Cost && a.Error <= b.Error)
                   && (a.Cost < b.Cost || a.Error < b.Error);
        }

        private List<Individual> TournamentSelection(List<Individual> population)
        {
            List<Individual> newPopulation = new List<Individual>();

            while (newPopulation.Count < populationSize)
            {
                Individual ind1 = population[rand.Next(population.Count)];
                Individual ind2 = population[rand.Next(population.Count)];

                // Rulettkerék szerű kiválasztás
                if (rand.NextDouble() < 0.5) // 50%-os esély
                {
                    newPopulation.Add(ind1);
                }
                else
                {
                    newPopulation.Add(ind2);
                }
            }

            return newPopulation;
        }

        private void RunNSGAII()
        {
            List<Individual> population = InitializePopulation();

            for (int generation = 0; generation < generations; generation++)
            {
                EvaluatePopulation(population);
                AssignParetoRanks(population);
                population = PerformNSGAIISelection(population);
                population = TournamentSelection(population);
                population = CrossoverAndMutate(population);

                this.Invoke((Action)(() => DrawParetoFront(population)));
            }

            EvaluatePopulation(population);
            var fronts = FastNonDominatedSort(population);
            this.Invoke((Action)(() => OutputParetoFront(population)));
        }

        private List<Individual> PerformNSGAIISelection(List<Individual> population)
        {
            List<List<Individual>> fronts = FastNonDominatedSort(population);
            CalculateCrowdingDistances(fronts);

            List<Individual> newPopulation = new List<Individual>();
            foreach (List<Individual> front in fronts)
            {
                front.Sort((a, b) => b.CrowdingDistance.CompareTo(a.CrowdingDistance));
                newPopulation.AddRange(front);
                if (newPopulation.Count >= populationSize)
                    break;
            }

            return newPopulation.Take(populationSize).ToList();
        }

        private List<List<Individual>> FastNonDominatedSort(List<Individual> population)
        {
            List<List<Individual>> fronts = new List<List<Individual>>();
            Dictionary<Individual, List<Individual>> dominatedSets = new Dictionary<Individual, List<Individual>>();
            Dictionary<Individual, int> dominationCounts = new Dictionary<Individual, int>();

            foreach (Individual ind in population)
            {
                dominatedSets[ind] = new List<Individual>();
                dominationCounts[ind] = 0;

                foreach (Individual other in population)
                {
                    if (Dominates(ind, other)) dominatedSets[ind].Add(other);
                    else if (Dominates(other, ind)) dominationCounts[ind]++;
                }

                if (dominationCounts[ind] == 0)
                {
                    if (fronts.Count == 0) fronts.Add(new List<Individual>());
                    fronts[0].Add(ind);
                    ind.Rank = 0;
                }
            }

            int i = 0;
            while (i < fronts.Count)
            {
                List<Individual> nextFront = new List<Individual>();
                foreach (Individual ind in fronts[i])
                {
                    foreach (Individual dominated in dominatedSets[ind])
                    {
                        dominationCounts[dominated]--;
                        if (dominationCounts[dominated] == 0)
                        {
                            nextFront.Add(dominated);
                            dominated.Rank = i + 1;
                        }
                    }
                }
                if (nextFront.Count > 0) fronts.Add(nextFront);
                i++;
            }
            return fronts;
        }

        private void CalculateCrowdingDistances(List<List<Individual>> fronts)
        {
            foreach (List<Individual> front in fronts)
            {
                int n = front.Count;
                if (n == 0) continue;

                foreach (Individual ind in front)
                    ind.CrowdingDistance = 0;

                for (int m = 0; m < 2; m++)
                {
                    front.Sort((a, b) => m == 0 ? a.Cost.CompareTo(b.Cost) : a.Error.CompareTo(b.Error));
                    front[0].CrowdingDistance = front[^1].CrowdingDistance = float.MaxValue;

                    for (int j = 1; j < n - 1; j++)
                    {
                        float range = (m == 0) ? (front[^1].Cost - front[0].Cost) : (front[^1].Error - front[0].Error);
                        if (range == 0) continue;

                        front[j].CrowdingDistance += (m == 0 ? (front[j + 1].Cost - front[j - 1].Cost) : (front[j + 1].Error - front[j - 1].Error)) / range;
                    }
                }
            }
        }

        private void EvaluatePopulation(List<Individual> population)
        {
            foreach (Individual individual in population)
            {
                individual.Cost = 0;
                individual.Error = 0;

                for (int i = 0; i < individual.Allocation.Count; i++)
                {
                    Worker worker = workers[i];
                    int pieces = individual.Allocation[i];
                    individual.Cost += pieces * worker.HourlyWage;
                    individual.Error += pieces * worker.ErrorRate;
                }
            }
        }

        private List<Individual> InitializePopulation()
        {
            List<Individual> population = new List<Individual>();
            for (int i = 0; i < populationSize; i++)
            {
                List<int> allocation = new List<int>(new int[workers.Count]);
                int remainingWorkPieces = numWorkPieces;

                while (remainingWorkPieces > 0)
                {
                    int workerIndex = rand.Next(workers.Count);
                    int pieces = rand.Next(1, Math.Min(remainingWorkPieces + 1, 10));

                    allocation[workerIndex] += pieces;
                    remainingWorkPieces -= pieces;
                }

                Individual individual = new Individual { Allocation = allocation };
                individual.CalculateCostAndError(workers);

                population.Add(individual);
            }
            return population;
        }

        private List<Individual> CrossoverAndMutate(List<Individual> population)
        {
            List<Individual> offspring = new List<Individual>();

            for (int i = 0; i < population.Count / 2; i++)
            {
                Individual parent1 = population[rand.Next(population.Count)];
                Individual parent2 = population[rand.Next(population.Count)];

                (Individual child1, Individual child2) = Crossover(parent1, parent2);

                Mutate(child1);
                Mutate(child2);

                child1.CalculateCostAndError(workers);
                child2.CalculateCostAndError(workers);

                offspring.Add(child1);
                offspring.Add(child2);
            }

            return offspring;
        }

        private (Individual, Individual) Crossover(Individual parent1, Individual parent2)
        {
            int numWorkers = parent1.Allocation.Count;
            if (numWorkers < 2) return (parent1, parent2);

            int crossoverPoint = rand.Next(1, numWorkers);

            List<int> child1Allocation = parent1.Allocation.Take(crossoverPoint)
                .Concat(parent2.Allocation.Skip(crossoverPoint)).ToList();
            List<int> child2Allocation = parent2.Allocation.Take(crossoverPoint)
                .Concat(parent1.Allocation.Skip(crossoverPoint)).ToList();

            return (new Individual { Allocation = child1Allocation }, new Individual { Allocation = child2Allocation });
        }

        private void Mutate(Individual individual)
        {
            int numWorkers = individual.Allocation.Count;
            if (numWorkers < 2) return;

            List<int> nonZeroIndexes = individual.Allocation
                .Select((value, index) => new { Value = value, Index = index })
                .Where(x => x.Value > 0)
                .Select(x => x.Index)
                .ToList();

            if (nonZeroIndexes.Count < 2) return;

            int index1 = nonZeroIndexes[rand.Next(nonZeroIndexes.Count)];
            int index2;
            do
            {
                index2 = rand.Next(numWorkers);
            } while (index1 == index2);

            int delta = rand.Next(1, Math.Min(individual.Allocation[index1], 10));

            individual.Allocation[index1] -= delta;
            individual.Allocation[index2] += delta;
        }

        private void DrawParetoFront(List<Individual> paretoFront)
        {
            if (paretoFront == null || paretoFront.Count == 0)
            {
                MessageBox.Show("No Pareto front found!");
                return;
            }

            Bitmap bitmap = new Bitmap(pbCanvas.Width, pbCanvas.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);

                float maxCost = paretoFront.Max(ind => ind.Cost);
                float minCost = paretoFront.Min(ind => ind.Cost);
                float maxError = paretoFront.Max(ind => ind.Error);
                float minError = paretoFront.Min(ind => ind.Error);

                float costRange = maxCost - minCost;
                float errorRange = maxError - minError;

                if (costRange == 0) costRange = 1;
                if (errorRange == 0) errorRange = 1;

                float padding = 10; // Extra padding to avoid boundary issues
                float scaleX = (pbCanvas.Width - 2 * padding) / costRange;
                float scaleY = (pbCanvas.Height - 2 * padding) / errorRange;

                foreach (Individual individual in paretoFront)
                {
                    float x = padding + (individual.Cost - minCost) * scaleX;
                    float y = pbCanvas.Height - padding - (individual.Error - minError) * scaleY;

                    g.FillEllipse(Brushes.Red, x - 3, y - 3, 6, 6);
                }
            }
            pbCanvas.Image = bitmap;
            pbCanvas.Refresh();
        }

        private void OutputParetoFront(List<Individual> paretoFront)
        {
            StringBuilder sb = new StringBuilder();

            // Csoportosítás rang szerint
            IEnumerable<IGrouping<int, Individual>> groupedByRank = paretoFront
                .GroupBy(ind => ind.Rank)
                .OrderBy(g => g.Key);

            foreach (IGrouping<int, Individual> group in groupedByRank)
            {
                sb.AppendLine($"Pareto Front {group.Key + 1}:");
                foreach (Individual individual in group)
                {
                    sb.AppendLine($"  Cost: {individual.Cost:F2}, Error: {individual.Error:F2}");
                    sb.AppendLine($"  Allocation: {string.Join(", ", individual.Allocation)}");
                    sb.AppendLine();
                }
            }

            MessageBox.Show(sb.ToString(), "Pareto Fronts");
        }
    }
}

/*
private void RunNSGAII()
{
    List<Individual> population = InitializePopulation();

    for (int generation = 0; generation < generations; generation++)
    {
        EvaluatePopulation(population);
        population = PerformNSGAIISelection(population);
        population = CrossoverAndMutate(population);

        int currentGeneration = generation;  // tárold el a generáció számát
        this.Invoke((Action)(() => DrawParetoFront(population, currentGeneration)));
    }

    EvaluatePopulation(population);
    this.Invoke((Action)(() => OutputParetoFront(population)));
}

private void DrawParetoFront(List<Individual> paretoFront, int generation)
{
    if (paretoFront == null || paretoFront.Count == 0)
    {
        MessageBox.Show("No Pareto front found!");
        return;
    }

    Bitmap bitmap = new Bitmap(pbCanvas.Width, pbCanvas.Height);
    using (Graphics g = Graphics.FromImage(bitmap))
    {
        g.Clear(Color.White);

        float maxCost = paretoFront.Max(ind => ind.Cost);
        float minCost = paretoFront.Min(ind => ind.Cost);
        float maxError = paretoFront.Max(ind => ind.Error);
        float minError = paretoFront.Min(ind => ind.Error);

        float costRange = maxCost - minCost;
        float errorRange = maxError - minError;

        if (costRange == 0) costRange = 1;
        if (errorRange == 0) errorRange = 1;

        float padding = 10;
        float scaleX = (pbCanvas.Width - 2 * padding) / costRange;
        float scaleY = (pbCanvas.Height - 2 * padding) / errorRange;

        // Több színpaletta, generáció szerint váltogatva:
        Color[][] colorPalettes = new Color[][]
        {
            new Color[] { Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Blue, Color.Indigo, Color.Violet },
            new Color[] { Color.DarkRed, Color.DarkOrange, Color.Gold, Color.DarkGreen, Color.DarkBlue, Color.MediumPurple, Color.DeepPink },
            new Color[] { Color.Crimson, Color.OrangeRed, Color.Goldenrod, Color.ForestGreen, Color.RoyalBlue, Color.MediumSlateBlue, Color.HotPink }
            // Több paletta is hozzáadható...
        };

        Color[] palette = colorPalettes[generation % colorPalettes.Length];

        for (int i = 0; i < paretoFront.Count; i++)
        {
            Individual individual = paretoFront[i];
            float x = padding + (individual.Cost - minCost) * scaleX;
            float y = pbCanvas.Height - padding - (individual.Error - minError) * scaleY;

            Color pointColor = palette[i % palette.Length];
            using (Brush brush = new SolidBrush(pointColor))
            {
                g.FillEllipse(brush, x - 4, y - 4, 8, 8); // kicsit nagyobb pöttyök
            }
        }
    }
    pbCanvas.Image = bitmap;
    pbCanvas.Refresh();
}
*/ 