using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;

namespace AudioVisualizer
{
    public partial class MainForm : Form
    {
        private SerialPort? serialPort;
        private WasapiLoopbackCapture? capture;
        private readonly int bandsCount = 16;
        private float[] bandValues;

        private Button btnStart;
        private Button btnStop;
        private ComboBox cmbComPorts;
        private Label lblStatus;
        private Button btnRefreshPorts;
        private Label label1;
        private Panel visualizerPanel;
        private CheckBox chkShowVisualizer;
        private System.Windows.Forms.Timer animationTimer;

        public MainForm()
        {
            InitializeComponent();
            InitializeComponents();
        }

        private void InitializeComponent()
        {
            this.btnStart = new Button();
            this.btnStop = new Button();
            this.cmbComPorts = new ComboBox();
            this.lblStatus = new Label();
            this.btnRefreshPorts = new Button();
            this.label1 = new Label();
            this.visualizerPanel = new Panel();
            this.chkShowVisualizer = new CheckBox();
            this.animationTimer = new System.Windows.Forms.Timer();

            this.SuspendLayout();

            // label1
            this.label1.AutoSize = true;
            this.label1.Location = new Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new Size(59, 13);
            this.label1.Text = "COM порт:";

            // cmbComPorts
            this.cmbComPorts.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbComPorts.FormattingEnabled = true;
            this.cmbComPorts.Location = new Point(77, 12);
            this.cmbComPorts.Name = "cmbComPorts";
            this.cmbComPorts.Size = new Size(121, 21);
            this.cmbComPorts.TabIndex = 0;

            // btnRefreshPorts
            this.btnRefreshPorts.Location = new Point(204, 10);
            this.btnRefreshPorts.Name = "btnRefreshPorts";
            this.btnRefreshPorts.Size = new Size(75, 23);
            this.btnRefreshPorts.Text = "Оновити";
            this.btnRefreshPorts.UseVisualStyleBackColor = true;
            this.btnRefreshPorts.Click += new EventHandler(this.btnRefreshPorts_Click);

            // btnStart
            this.btnStart.Location = new Point(12, 45);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new Size(75, 23);
            this.btnStart.Text = "Старт";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new EventHandler(this.btnStart_Click);

            // btnStop
            this.btnStop.Enabled = false;
            this.btnStop.Location = new Point(93, 45);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new Size(75, 23);
            this.btnStop.Text = "Стоп";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new EventHandler(this.btnStop_Click);

            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold);
            this.lblStatus.Location = new Point(174, 50);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new Size(91, 13);
            this.lblStatus.Text = "Статус: Стоп";
            this.lblStatus.ForeColor = Color.Red;

            // chkShowVisualizer
            this.chkShowVisualizer.AutoSize = true;
            this.chkShowVisualizer.Location = new Point(12, 75);
            this.chkShowVisualizer.Name = "chkShowVisualizer";
            this.chkShowVisualizer.Size = new Size(121, 17);
            this.chkShowVisualizer.Text = "Показати візуалізацію";
            this.chkShowVisualizer.UseVisualStyleBackColor = true;
            this.chkShowVisualizer.CheckedChanged += new EventHandler(this.chkShowVisualizer_CheckedChanged);

            // visualizerPanel
            this.visualizerPanel.Location = new Point(12, 100);
            this.visualizerPanel.Size = new Size(460, 200);
            this.visualizerPanel.BackColor = Color.Black;
            this.visualizerPanel.BorderStyle = BorderStyle.FixedSingle;
            this.visualizerPanel.Visible = false;
            this.visualizerPanel.Paint += new PaintEventHandler(this.visualizerPanel_Paint);

            // animationTimer
            this.animationTimer.Interval = 50;
            this.animationTimer.Tick += new EventHandler(this.animationTimer_Tick);

            // MainForm
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(484, 311);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmbComPorts);
            this.Controls.Add(this.btnRefreshPorts);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.chkShowVisualizer);
            this.Controls.Add(this.visualizerPanel);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "Audio Visualizer";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void InitializeComponents()
        {
            // Ініціалізація масиву смуг
            bandValues = new float[bandsCount];

            // Налаштування COM порта
            InitializeSerialPort();

            // Налаштування аудіо захвату
            InitializeAudioCapture();

            // Заповнення вибору COM портів
            RefreshComPorts();
        }

        private void InitializeSerialPort()
        {
            serialPort = new SerialPort();
            serialPort.BaudRate = 115200;
            serialPort.DataBits = 8;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.Handshake = Handshake.None;
        }

        private void InitializeAudioCapture()
        {
            try
            {
                capture = new WasapiLoopbackCapture();
                capture.DataAvailable += Capture_DataAvailable;
                capture.RecordingStopped += Capture_RecordingStopped;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка ініціалізації аудіо: {ex.Message}");
            }
        }

        private void RefreshComPorts()
        {
            cmbComPorts.Items.Clear();
            string[] ports = SerialPort.GetPortNames();
            cmbComPorts.Items.AddRange(ports);
            if (ports.Length > 0)
                cmbComPorts.SelectedIndex = 0;
        }

        private void Capture_DataAvailable(object? sender, WaveInEventArgs e)
        {
            // Конвертуємо byte[] в float[]
            float[] audioBuffer = new float[e.BytesRecorded / 4];
            Buffer.BlockCopy(e.Buffer, 0, audioBuffer, 0, e.BytesRecorded);

            // Аналізуємо спектр
            AnalyzeSpectrum(audioBuffer);

            // Відправляємо дані на ESP
            SendDataToESP();

            // Оновлюємо візуалізацію
            UpdateVisualization();
        }

        private void AnalyzeSpectrum(float[] audioBuffer)
        {
            if (audioBuffer.Length < 1024) return;

            // Застосовуємо FFT
            int fftPoints = 1024;
            var fftBuffer = new Complex[fftPoints];

            for (int i = 0; i < fftPoints; i++)
            {
                fftBuffer[i].X = (float)(audioBuffer[i] * FastFourierTransform.HammingWindow(i, fftPoints));
                fftBuffer[i].Y = 0;
            }

            FastFourierTransform.FFT(true, (int)Math.Log(fftPoints, 2.0), fftBuffer);

            // Розподіляємо по смугах (логаріфмічно)
            float[] bandBounds = {
                50, 100, 150, 200, 300, 400, 500, 600,
                800, 1000, 1500, 2000, 3000, 4000, 6000, 8000
            };

            for (int i = 0; i < bandsCount; i++)
            {
                float lowerFreq = i == 0 ? 20 : bandBounds[i - 1];
                float upperFreq = bandBounds[i];

                float magnitude = CalculateBandMagnitude(fftBuffer, lowerFreq, upperFreq, 44100, fftPoints);
                bandValues[i] = magnitude * 1000f; // Підсилюємо для кращої видимості
            }
        }

        private float CalculateBandMagnitude(Complex[] fftBuffer, float lowFreq, float highFreq, int sampleRate, int fftSize)
        {
            int lowIndex = (int)(lowFreq * fftSize / sampleRate);
            int highIndex = (int)(highFreq * fftSize / sampleRate);

            lowIndex = Math.Max(1, lowIndex); // Уникаємо DC offset
            highIndex = Math.Min(fftSize / 2 - 1, highIndex);

            float sum = 0;
            for (int i = lowIndex; i <= highIndex; i++)
            {
                float magnitude = (float)Math.Sqrt(fftBuffer[i].X * fftBuffer[i].X + fftBuffer[i].Y * fftBuffer[i].Y);
                sum += magnitude;
            }

            return sum / (highIndex - lowIndex + 1);
        }

        private void SendDataToESP()
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    // Нормалізуємо значення до 0-100
                    int[] normalizedBands = new int[bandsCount];
                    float maxValue = bandValues.Max();

                    if (maxValue > 0)
                    {
                        for (int i = 0; i < bandsCount; i++)
                        {
                            normalizedBands[i] = (int)Math.Min(100, (bandValues[i] / maxValue) * 100);
                        }
                    }

                    // Формуємо рядок даних
                    string dataString = string.Join(",", normalizedBands);

                    // Відправляємо на ESP
                    serialPort.WriteLine(dataString);
                }
                catch (Exception ex)
                {
                    // Ігноруємо помилки відправки
                    System.Diagnostics.Debug.WriteLine($"Send error: {ex.Message}");
                }
            }
        }

        private void UpdateVisualization()
        {
            if (chkShowVisualizer.Checked && visualizerPanel.Visible)
            {
                // Запускаємо перемальовування в UI thread
                visualizerPanel.BeginInvoke(new Action(() => visualizerPanel.Invalidate()));
            }
        }

        private void visualizerPanel_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.Black);

            int panelWidth = visualizerPanel.Width;
            int panelHeight = visualizerPanel.Height;
            int barWidth = (panelWidth - 20) / bandsCount;
            int maxBarHeight = panelHeight - 20;

            // Малюємо смуги аквалайзера
            for (int i = 0; i < bandsCount; i++)
            {
                float normalizedValue = Math.Min(1.0f, bandValues[i] / 50f); // Нормалізація
                int barHeight = (int)(normalizedValue * maxBarHeight);

                if (barHeight > 0)
                {
                    // Градієнт кольору від зеленого до червоного
                    Color barColor = GetColorForValue(normalizedValue);

                    using (Brush brush = new SolidBrush(barColor))
                    {
                        int x = 10 + i * barWidth;
                        int y = panelHeight - barHeight - 10;

                        // Малюємо стовпчик
                        g.FillRectangle(brush, x, y, barWidth - 2, barHeight);

                        // Додаємо обводку
                        g.DrawRectangle(Pens.White, x, y, barWidth - 2, barHeight);
                    }
                }
            }

            // Малюємо сітку
            DrawGrid(g, panelWidth, panelHeight);

            // Малюємо підписи
            DrawLabels(g, panelWidth, panelHeight);
        }

        private Color GetColorForValue(float value)
        {
            // Градієнт: зелений -> жовтий -> червоний
            if (value < 0.5f)
            {
                // Зелений до жовтого
                int green = 255;
                int red = (int)(255 * (value * 2));
                return Color.FromArgb(red, green, 0);
            }
            else
            {
                // Жовтий до червоного
                int red = 255;
                int green = (int)(255 * (1 - (value - 0.5f) * 2));
                return Color.FromArgb(red, green, 0);
            }
        }

        private void DrawGrid(Graphics g, int width, int height)
        {
            using (Pen gridPen = new Pen(Color.FromArgb(50, 255, 255, 255)))
            {
                // Вертикальні лінії
                for (int i = 1; i < 4; i++)
                {
                    int x = width * i / 4;
                    g.DrawLine(gridPen, x, 10, x, height - 10);
                }

                // Горизонтальні лінії
                for (int i = 1; i < 4; i++)
                {
                    int y = height * i / 4;
                    g.DrawLine(gridPen, 10, y, width - 10, y);
                }
            }
        }

        private void DrawLabels(Graphics g, int width, int height)
        {
            using (Brush textBrush = new SolidBrush(Color.White))
            using (Font smallFont = new Font("Arial", 8))
            {
                // Підписи частот
                string[] freqLabels = { "50Hz", "200Hz", "800Hz", "3kHz", "8kHz" };
                for (int i = 0; i < freqLabels.Length; i++)
                {
                    int x = 10 + (width - 20) * i / (freqLabels.Length - 1);
                    g.DrawString(freqLabels[i], smallFont, textBrush, x - 15, height - 25);
                }

                // Підписи рівнів
                string[] levelLabels = { "100%", "75%", "50%", "25%", "0%" };
                for (int i = 0; i < levelLabels.Length; i++)
                {
                    int y = 10 + (height - 20) * i / (levelLabels.Length - 1);
                    g.DrawString(levelLabels[i], smallFont, textBrush, 5, y - 6);
                }
            }
        }

        private void animationTimer_Tick(object? sender, EventArgs e)
        {
            // Плавне зменшення значень для анімації
            for (int i = 0; i < bandsCount; i++)
            {
                bandValues[i] *= 0.85f; // Плавне падіння
                if (bandValues[i] < 0.1f) bandValues[i] = 0;
            }

            UpdateVisualization();
        }

        private void chkShowVisualizer_CheckedChanged(object? sender, EventArgs e)
        {
            visualizerPanel.Visible = chkShowVisualizer.Checked;

            if (chkShowVisualizer.Checked)
            {
                this.ClientSize = new Size(484, 311);
                animationTimer.Start();
            }
            else
            {
                this.ClientSize = new Size(484, 100);
                animationTimer.Stop();
            }
        }

        private void Capture_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                MessageBox.Show($"Помилка аудіо: {e.Exception.Message}");
            }
        }

        private void btnStart_Click(object? sender, EventArgs e)
        {
            try
            {
                // Відкриваємо COM порт
                if (cmbComPorts.SelectedItem != null && serialPort != null)
                {
                    serialPort.PortName = cmbComPorts.SelectedItem.ToString() ?? "";
                    serialPort.Open();
                }

                // Запускаємо захват аудіо
                if (capture != null)
                {
                    capture.StartRecording();
                }

                // Запускаємо таймер анімації
                animationTimer.Start();

                UpdateUI(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка запуску: {ex.Message}");
            }
        }

        private void btnStop_Click(object? sender, EventArgs e)
        {
            StopCapture();
        }

        private void StopCapture()
        {
            if (capture != null && capture.CaptureState == CaptureState.Capturing)
            {
                capture.StopRecording();
            }

            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
            }

            animationTimer.Stop();
            UpdateUI(false);
        }

        private void UpdateUI(bool isRunning)
        {
            btnStart.Enabled = !isRunning;
            btnStop.Enabled = isRunning;
            cmbComPorts.Enabled = !isRunning;
            lblStatus.Text = isRunning ? "Статус: Запущено" : "Статус: Зупинено";
            lblStatus.ForeColor = isRunning ? Color.Green : Color.Red;
        }

        private void btnRefreshPorts_Click(object? sender, EventArgs e)
        {
            RefreshComPorts();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                serialPort?.Close();
                serialPort?.Dispose();
                capture?.Dispose();
                animationTimer?.Stop();
                animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}