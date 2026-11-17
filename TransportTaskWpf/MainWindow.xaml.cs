using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace TransportTaskWpf
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<TransportRow> InputData { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            LoadVariant6();
        }

        // Загрузка данных 6 варианта из задания
        private void LoadVariant6()
        {
            InputData = new ObservableCollection<TransportRow>
            {
                new TransportRow { Header = "A1", B1 = 24, B2 = 50, B3 = 55, B4 = 27, B5 = 16, Supply = 200 },
                new TransportRow { Header = "A2", B1 = 50, B2 = 47, B3 = 23, B4 = 17, B5 = 21, Supply = 350 },
                new TransportRow { Header = "A3", B1 = 35, B2 = 59, B3 = 55, B4 = 27, B5 = 41, Supply = 300 }
            };

            dgInput.ItemsSource = InputData;
            dgInput.LoadingRow += (s, e) => e.Row.Header = ((TransportRow)e.Row.DataContext).Header;

            txtB1.Text = "270";
            txtB2.Text = "130";
            txtB3.Text = "190";
            txtB4.Text = "150";
            txtB5.Text = "110";
        }

        private void BtnLoadVariant_Click(object sender, RoutedEventArgs e)
        {
            LoadVariant6();
            dgResult.ItemsSource = null;
            txtResultInfo.Text = "Данные Варианта 6 загружены.";
        }

        // Получение матриц из интерфейса и проверка баланса
        private bool TryGetData(out int[,] costs, out int[] supply, out int[] demand)
        {
            int rows = InputData.Count;
            int cols = 5;
            costs = new int[rows, cols];
            supply = new int[rows];
            demand = new int[cols];

            try
            {
                for (int i = 0; i < rows; i++)
                {
                    costs[i, 0] = InputData[i].B1;
                    costs[i, 1] = InputData[i].B2;
                    costs[i, 2] = InputData[i].B3;
                    costs[i, 3] = InputData[i].B4;
                    costs[i, 4] = InputData[i].B5;
                    supply[i] = InputData[i].Supply;
                }

                demand[0] = int.Parse(txtB1.Text);
                demand[1] = int.Parse(txtB2.Text);
                demand[2] = int.Parse(txtB3.Text);
                demand[3] = int.Parse(txtB4.Text);
                demand[4] = int.Parse(txtB5.Text);

                int sumSupply = 0, sumDemand = 0;
                foreach (int s in supply) sumSupply += s;
                foreach (int d in demand) sumDemand += d;

                if (sumSupply != sumDemand)
                {
                    MessageBox.Show($"Задача не сбалансирована!\nСумма запасов: {sumSupply}\nСумма потребностей: {sumDemand}",
                        "Ошибка баланса", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                return true;
            }
            catch
            {
                MessageBox.Show("Проверьте корректность введенных чисел.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // 1. Метод северо-западного угла
        private void BtnNorthWest_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetData(out int[,] costs, out int[] supply, out int[] demand)) return;

            int rows = supply.Length;
            int cols = demand.Length;
            int[,] plan = new int[rows, cols];

            int i = 0, j = 0;
            while (i < rows && j < cols)
            {
                int allocation = Math.Min(supply[i], demand[j]);
                plan[i, j] = allocation;
                supply[i] -= allocation;
                demand[j] -= allocation;

                if (supply[i] == 0) i++;
                else j++;
            }

            DisplayResult(plan, costs, "Метод северо-западного угла");
        }

        // 2. Метод минимальных элементов
        private void BtnMinElement_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetData(out int[,] costs, out int[] supply, out int[] demand)) return;

            int rows = supply.Length;
            int cols = demand.Length;
            int[,] plan = new int[rows, cols];
            bool[] rowDone = new bool[rows];
            bool[] colDone = new bool[cols];

            while (true)
            {
                int minCost = int.MaxValue;
                int minI = -1, minJ = -1;

                // Поиск клетки с минимальным тарифом среди доступных
                for (int i = 0; i < rows; i++)
                {
                    if (rowDone[i]) continue;
                    for (int j = 0; j < cols; j++)
                    {
                        if (colDone[j]) continue;
                        if (costs[i, j] < minCost)
                        {
                            minCost = costs[i, j];
                            minI = i;
                            minJ = j;
                        }
                    }
                }

                if (minI == -1) break; // Все клетки распределены

                int allocation = Math.Min(supply[minI], demand[minJ]);
                plan[minI, minJ] = allocation;
                supply[minI] -= allocation;
                demand[minJ] -= allocation;

                if (supply[minI] == 0) rowDone[minI] = true;
                if (demand[minJ] == 0) colDone[minJ] = true;
            }

            DisplayResult(plan, costs, "Метод минимальных элементов");
        }

        // Вывод плана в таблицу и расчет целевой функции F(x)
        private void DisplayResult(int[,] plan, int[,] costs, string methodName)
        {
            var resultRows = new ObservableCollection<TransportRow>();
            int totalCost = 0;

            for (int i = 0; i < plan.GetLength(0); i++)
            {
                resultRows.Add(new TransportRow
                {
                    Header = $"A{i + 1}",
                    B1 = plan[i, 0],
                    B2 = plan[i, 1],
                    B3 = plan[i, 2],
                    B4 = plan[i, 3],
                    B5 = plan[i, 4]
                });

                for (int j = 0; j < plan.GetLength(1); j++)
                {
                    totalCost += plan[i, j] * costs[i, j];
                }
            }

            dgResult.ItemsSource = resultRows;
            dgResult.LoadingRow += (s, e) => e.Row.Header = ((TransportRow)e.Row.DataContext).Header;

            txtResultInfo.Text = $"{methodName} — Итоговая стоимость (F): {totalCost}";
        }
    }

    // Модель строки таблицы
    public class TransportRow
    {
        public string Header { get; set; }
        public int B1 { get; set; }
        public int B2 { get; set; }
        public int B3 { get; set; }
        public int B4 { get; set; }
        public int B5 { get; set; }
        public int Supply { get; set; }
    }
}
