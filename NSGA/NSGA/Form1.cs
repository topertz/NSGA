using System.Text;

namespace NSGAI
{
    public partial class Form1 : Form
    {
        private Random rand = new Random();
        private List<Worker> workers = new List<Worker>();
        private int numWorkPieces;
        private int populationSize = 100;
        private int generations = 500;
        public Form1()
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
            foreach (Worker worker in workers)
            {
                Console.WriteLine($"Worker ID: {worker.Id}, Wage: {worker.HourlyWage}, ErrorRate: {worker.ErrorRate}");
            }
            try
            {
                workers = await LoadWorkersFromFileAsync(filePath);
                if (workers.Count > 0)
                {
                    btnRunAlgorithm.Enabled = true; // Csak akkor engedélyezzük, ha sikerült betölteni a munkásokat
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
                        throw new Exception("Line {i + 1} contains invalid data.");

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
                MessageBox.Show("Error loading worker data: ", ex.Message);
            }

            return workers;
        }

        private async void btnRunAlgorithm_Click(object? sender, EventArgs e)
        {
            btnRunAlgorithm.Enabled = false;

            try
            {
                await Task.Run(() => RunNSGAI());
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
            List<List<Individual>> fronts = new List<List<Individual>>();
            Dictionary<Individual, List<Individual>> dominationMap = new Dictionary<Individual, List<Individual>>();
            Dictionary<Individual, int> dominationCount = new Dictionary<Individual, int>();

            foreach (Individual p in population)
            {
                dominationMap[p] = new List<Individual>();
                dominationCount[p] = 0;

                foreach (Individual q in population)
                {
                    if (Dominates(p, q))
                    {
                        dominationMap[p].Add(q);
                    }
                    else if (Dominates(q, p))
                    {
                        dominationCount[p]++;
                    }
                }

                if (dominationCount[p] == 0)
                {
                    p.Rank = 0;
                    if (fronts.Count == 0)
                        fronts.Add(new List<Individual>());
                    fronts[0].Add(p);
                }
            }

            int i = 0;
            while (i < fronts.Count)
            {
                List<Individual> nextFront = new List<Individual>();
                foreach (Individual p in fronts[i])
                {
                    foreach (Individual q in dominationMap[p])
                    {
                        dominationCount[q]--;
                        if (dominationCount[q] == 0)
                        {
                            q.Rank = i + 1;
                            nextFront.Add(q);
                        }
                    }
                }

                if (nextFront.Count > 0)
                    fronts.Add(nextFront);

                i++;
            }
        }

        private bool Dominates(Individual a, Individual b)
        {
            return (a.Cost <= b.Cost && a.Error <= b.Error) && (a.Cost < b.Cost || a.Error < b.Error);
        }

        private List<Individual> TournamentSelection(List<Individual> population)
        {
            List<Individual> newPopulation = new List<Individual>();

            while (newPopulation.Count < populationSize)
            {
                Individual ind1 = population[rand.Next(population.Count)];
                Individual ind2 = population[rand.Next(population.Count)];

                newPopulation.Add(ind1.Rank < ind2.Rank ? ind1 : ind2);
            }

            return newPopulation;
        }

        private void RunNSGAI()
        {
            List<Individual> population = InitializePopulation();

            for (int generation = 0; generation < generations; generation++)
            {
                EvaluatePopulation(population);
                AssignParetoRanks(population);
                List<Individual> newPopulation = SelectNewPopulation(population);
                population = CrossoverAndMutate(newPopulation);

                this.Invoke((Action)(() =>
                {
                    DrawParetoFront(population);
                }));
            }

            EvaluatePopulation(population);
            AssignParetoRanks(population);
            this.Invoke((Action)(() =>
            {
                OutputParetoFront(population);
            }));
        }

        private List<Individual> PerformNSGA1Selection(List<Individual> population)
        {
            // First, we perform non-dominated sorting
            List<List<Individual>> fronts = NonDominatedSort(population);

            // Create a new population list to store selected individuals
            List<Individual> newPopulation = new List<Individual>();

            // Iterate through the sorted fronts and add individuals to the new population
            foreach (List<Individual> front in fronts)
            {
                // If adding the whole front exceeds the population size, we stop
                if (newPopulation.Count + front.Count > populationSize)
                {
                    break;
                }

                // Add all individuals from this front
                newPopulation.AddRange(front);
            }

            // If the last front exceeds the population size, select individuals randomly to match the population size
            if (newPopulation.Count < populationSize)
            {
                int remainingCount = populationSize - newPopulation.Count;
                List<Individual> lastFront = fronts.Last();
                Random random = new Random();

                // Select random individuals from the last front
                for (int i = 0; i < remainingCount; i++)
                {
                    newPopulation.Add(lastFront[random.Next(lastFront.Count)]);
                }
            }

            return newPopulation;
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

        private List<Individual> SelectNewPopulation(List<Individual> population)
        {
            population.Sort((a, b) => a.Cost.CompareTo(b.Cost));
            return population.Take(populationSize).ToList();
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

            int changeAmount = rand.Next(1, individual.Allocation[index1] + 1);

            individual.Allocation[index1] -= changeAmount;
            individual.Allocation[index2] += changeAmount;

            Console.WriteLine("Mutation:  -> , Change: ", index1, index2, changeAmount);
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

        private List<List<Individual>> NonDominatedSort(List<Individual> population)
        {
            List<List<Individual>> fronts = new List<List<Individual>>();
            Dictionary<Individual, List<Individual>> dominates = new Dictionary<Individual, List<Individual>>();
            Dictionary<Individual, int> dominationCount = new Dictionary<Individual, int>();

            foreach (Individual p in population)
            {
                dominates[p] = new List<Individual>();
                dominationCount[p] = 0;
            }

            List<Individual> firstFront = new List<Individual>();

            foreach (Individual p in population)
            {
                foreach (Individual q in population)
                {
                    if (Dominates(p, q))
                    {
                        dominates[p].Add(q);
                    }
                    else if (Dominates(q, p))
                    {
                        dominationCount[p]++;
                    }
                }

                if (dominationCount[p] == 0)
                {
                    p.Rank = 0;
                    firstFront.Add(p);
                }
            }

            fronts.Add(firstFront);
            int i = 0;
            while (fronts[i].Count > 0)
            {
                List<Individual> nextFront = new List<Individual>();

                foreach (Individual p in fronts[i])
                {
                    foreach (Individual q in dominates[p])
                    {
                        dominationCount[q]--;
                        if (dominationCount[q] == 0)
                        {
                            q.Rank = i + 1;
                            nextFront.Add(q);
                        }
                    }
                }

                i++;
                fronts.Add(nextFront);
            }

            // Az utolsó front üres lesz, azt eltávolítjuk
            if (fronts[fronts.Count - 1].Count == 0)
            {
                fronts.RemoveAt(fronts.Count - 1);
            }

            return fronts;
        }
    }
}

/*
private List<List<Individual>> NonDominatedSort(List<Individual> population)
{
    List<List<Individual>> fronts = new List<List<Individual>>();
    Dictionary<Individual, List<Individual>> dominates = new Dictionary<Individual, List<Individual>>();
    Dictionary<Individual, int> dominationCount = new Dictionary<Individual, int>();

    foreach (Individual p in population)
    {
        dominates[p] = new List<Individual>();
        dominationCount[p] = 0;
    }

    List<Individual> firstFront = new List<Individual>();

    foreach (Individual p in population)
    {
        foreach (Individual q in population)
        {
            if (Dominates(p, q))
            {
                dominates[p].Add(q);
            }
            else if (Dominates(q, p))
            {
                dominationCount[p]++;
            }
        }

        if (dominationCount[p] == 0)
        {
            p.Rank = 0;
            firstFront.Add(p);
        }
    }

    fronts.Add(firstFront);
    int i = 0;
    while (fronts[i].Count > 0)
    {
        List<Individual> nextFront = new List<Individual>();

        foreach (Individual p in fronts[i])
        {
            foreach (Individual q in dominates[p])
            {
                dominationCount[q]--;
                if (dominationCount[q] == 0)
                {
                    q.Rank = i + 1;
                    nextFront.Add(q);
                }
            }
        }

        i++;
        fronts.Add(nextFront);
    }

    // Az utolsó front üres lesz, azt eltávolítjuk
    if (fronts[fronts.Count - 1].Count == 0)
    {
        fronts.RemoveAt(fronts.Count - 1);
    }

    return fronts;
}

private void DrawParetoFront(List<List<Individual>> fronts)
{
    if (fronts == null || fronts.Count == 0)
    {
        MessageBox.Show("No Pareto fronts found!");
        return;
    }

    Bitmap bitmap = new Bitmap(pbCanvas.Width, pbCanvas.Height);
    using (Graphics g = Graphics.FromImage(bitmap))
    {
        g.Clear(Color.White);

        // Minden egyedet egy listából összefûzünk, hogy kiszámoljuk a min/max értékeket
        List<Individual> allIndividuals = fronts.SelectMany(f => f).ToList();

        float maxCost = allIndividuals.Max(ind => ind.Cost);
        float minCost = allIndividuals.Min(ind => ind.Cost);
        float maxError = allIndividuals.Max(ind => ind.Error);
        float minError = allIndividuals.Min(ind => ind.Error);

        float costRange = maxCost - minCost;
        float errorRange = maxError - minError;

        if (costRange == 0) costRange = 1;
        if (errorRange == 0) errorRange = 1;

        float padding = 10; // Extra padding to avoid boundary issues
        float scaleX = (pbCanvas.Width - 2 * padding) / costRange;
        float scaleY = (pbCanvas.Height - 2 * padding) / errorRange;

        // Színek listája a frontok megjelenítéséhez (ha több front, ismétlõdnek)
        Brush[] brushes = new Brush[]
        {
            Brushes.Red,
            Brushes.Blue,
            Brushes.Green,
            Brushes.Orange,
            Brushes.Purple,
            Brushes.Brown,
            Brushes.Magenta,
            Brushes.Cyan
        };

        for (int i = 0; i < fronts.Count; i++)
        {
            List<Individual> front = fronts[i];
            Brush brush = brushes[i % brushes.Length];

            foreach (Individual individual in front)
            {
                float x = padding + (individual.Cost - minCost) * scaleX;
                float y = pbCanvas.Height - padding - (individual.Error - minError) * scaleY;

                g.FillEllipse(brush, x - 4, y - 4, 8, 8);
            }
        }
    }
    pbCanvas.Image = bitmap;
    pbCanvas.Refresh();
}

private void RunNSGAI()
{
    List<Individual> population = InitializePopulation();

    for (int generation = 0; generation < generations; generation++)
    {
        EvaluatePopulation(population);

        // Többfrontos rangsorolás
        List<List<Individual>> fronts = NonDominatedSort(population);

        // Új populáció kiválasztása frontok alapján (ez csak egy egyszerû példa, lehet fejleszteni)
        List<Individual> newPopulation = new List<Individual>();
        int frontIndex = 0;
        while (newPopulation.Count + fronts[frontIndex].Count <= populationSize)
        {
            newPopulation.AddRange(fronts[frontIndex]);
            frontIndex++;
            if (frontIndex >= fronts.Count)
                break;
        }
        // Ha még kevés, töltsük fel a következõ front elemeivel
        if (newPopulation.Count < populationSize && frontIndex < fronts.Count)
        {
            List<Individual> sortedFront = fronts[frontIndex].OrderBy(ind => ind.Cost).ToList();
            int needed = populationSize - newPopulation.Count;
            newPopulation.AddRange(sortedFront.Take(needed));
        }

        population = CrossoverAndMutate(newPopulation);

        this.Invoke((Action)(() =>
        {
            DrawParetoFront(fronts);
        }));
    }

    EvaluatePopulation(population);
    List<List<Individual>> finalFronts = NonDominatedSort(population);

    this.Invoke((Action)(() =>
    {
        // Például az elsõ front kiíratása
        OutputParetoFront(finalFronts[0]);
    }));
}
*/