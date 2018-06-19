using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Numerics;

namespace MFCC
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Вычисляет значение оконной функции для узла j
        /// </summary>
        /// <param name="j"></param>
        /// <returns></returns>
        public double HammingWindow(int j)
        {
            double x = 0.53836 - 0.46164 * Math.Cos(2 * Math.PI * j / 1023);
            return x;
        }
        double ConvertToMel(double freq)
        {

            double mel = 1127 * Math.Log(1 + freq / 700);
            return mel;
        }
        double ConvertFromMel(double mel)
        {
            double freq = 700 * (Math.Pow(Math.E, mel / 1127) - 1);
            return freq;
        }
        /// <summary>
        /// Читает данные Wav-файла
        /// </summary>
        /// <param name="wavePath">путь к файлу</param>
        /// <returns>Массив амплитуд в интервале +-32768</returns>
        public double[] ReadDataFromExternalSource(string wavePath)
        {
            Double[] data;
            byte[] wave;
            System.IO.FileStream WaveFile = System.IO.File.OpenRead(wavePath);
            wave = new byte[WaveFile.Length];
            data = new Double[(wave.Length - 44) / 2];//shifting the headers out of the PCM data;
            WaveFile.Read(wave, 0, Convert.ToInt32(WaveFile.Length));//read the wave file into the wave variable
                                                                     /***********Converting and PCM accounting***************/
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (BitConverter.ToInt16(wave, 44 + i * 2));// 65536.0;

            }
            return data;
        }
        /// <summary>
        /// Нормализация сигнала?
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        double [] Pre_Emphasis (double[] x)
        {
            double[] y = new double[x.Length];
            y[0] = x[0] * 0.98;
            for (int i=1;i<x.Length;i++)
            {
                y[i] = x[i] - 0.97 * x[i - 1];
            }
            return y;
        }
        /// <summary>
        /// Разбиваем на кадры
        /// </summary>
        /// <param name="data"></param>
        /// <param name="size_of_frame"></param>
        /// <returns>номер каджра и кадр</returns>
        double [,] Frame_Blocking (double [] data,int size_of_frame)
        {
            int k = 0;
            double[,] data1 = new double[data.Length / (size_of_frame/2) - 1, size_of_frame];
            for (int i = 0; i < data.Length / (size_of_frame/2) - 1; i++)
            {
                if (data.Length - size_of_frame < size_of_frame) break;
                for (int j = 0; j < size_of_frame; j++)
                {
                    
                    data1[i, j] = data[j + k];

                }

                if (i != data.Length / (size_of_frame / 2) - 1) k += (size_of_frame / 2);
                else break;
            }
            return data1;
        }
        double[,] HammingWindow_using (double [,] data)
        {
            for (int i = 0; i < data.GetLength(0); i++)
                for (int j = 0; j < data.GetLength(1); j++)
                    data[i,j] *= HammingWindow(j);
            return data;
        }

        /// <summary>
        /// Преобразование Фурье
        /// </summary>
        /// <param name="data">Входные данные</param>
        /// <returns>Модуль фурье-образа</returns>
        public double [] FFT (double [] data)
        {
            double[] fourierraw = new double[data.GetLength(0)];
            Complex[] fourierCmplxRaw = new Complex[data.GetLength(0)];
            double x = 0;
            double sample;
            for (int i=0;i<data.GetLength(0);i++)
            {
                fourierCmplxRaw[i] = new Complex(0, 0);
                for (int j=0;j<data.GetLength(0);j++)
                {
                    sample = data[j];
                    x = (double)-2 * Math.PI * i * j / (double)data.GetLength(0);
                    Complex f = new Complex(Math.Cos(x), Math.Sin(x));
                    f *= sample;
                    fourierCmplxRaw[i] += f;
                }
                fourierraw[i] = fourierCmplxRaw[i].Magnitude;

            }
            return fourierraw;
        }
        double[] Filters(int furLength, double freqMin, double freqMax, int framesize, double freq)
        {
            double[] fb = new double[furLength + 2];

            fb[0] = ConvertToMel(freqMin);
            fb[furLength + 1] = ConvertToMel(freqMax);
            for (int i = 1; i < furLength + 1; i++)
            {
                fb[i] = fb[0] + i * (fb[furLength + 1] - fb[0]) / (furLength + 1);
            }
            for (int i = 0; i < furLength + 2; i++)
            {
                fb[i] = Math.Floor((framesize + 1) * ConvertFromMel(fb[i])) / (double)freq;
            }
            return fb;
        }
        /// <summary>
        /// Массив треуголниых фильтров.
        /// </summary>
        /// <param name="f">точки</param>
        /// <param name="datalength">количество кадров</param>
        /// <param name="hlenght">Количествто коэффициентов, которые будем получать</param>
        /// <returns></returns>
        double[,] FilterBanks(double[] f, int datalength, int hlenght)
        {
            double[,] h = new double[f.Length, datalength];
            for (int i = 1; i < hlenght + 1; i++)
            {
                for (int j = 0; j < datalength; j++)
                {
                    if (j < f[i - 1] || j > f[i + 1])
                        h[i - 1, j] = 0;
                    else
                        if (f[i - 1] <= j && j < f[i])
                        h[i - 1, j] = (j - f[i - 1]) / (f[i] - f[i - 1]);
                    else
                            if (f[i] <= j && j <= f[i + 1])
                        h[i - 1, j] = (f[i + 1] - j) / (f[i + 1] - f[i]);
                }

            }
            return h;
        }
        /// <summary>
        /// Собственно 
        /// </summary>
        /// <param name="fur">результат fft</param>
        /// <param name="h">массив после применения фильтров</param>
        /// <param name="flength">количество кэффициентов</param>
        /// <param name="datalength">размер кадра</param>
        /// <returns></returns>
        double[] LogEnrgSp(double[] fur, double[,] h, int flength, int datalength)
        {
            double[] mk = new double[flength];

            for (int i = 1; i < flength; i++)
            {
                mk[i] = 0;
                for (int j = 0; j < datalength; j++)
                {
                    mk[i] += Math.Abs(Math.Pow(fur[j], 2)) * h[i, j];
                }
                if (mk[i] != 0)
                    mk[i] = Math.Log(mk[i]);
                // textBox1.Text += Convert.ToString(mk[i]) + ", ";
            }

            return mk;
        }

        double[] CosPreobr(double[] mk)
        {
            double[] mkk = new double[mk.Length];
            for (int i = 0; i < mkk.Length; i++)
            {
                mkk[i] = 0;
                for (int j = 0; j < mkk.Length; j++)
                {
                    mkk[i] += mk[j] * Math.Cos(Math.PI * i * (j + 0.5) / mkk.Length);
                }
            }
            return mkk;
        }
        /// <summary>
        /// Mel Freaquency Cepstrum Coefficietns
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        double [] MFCC(string path)
        {
            double[] data = ReadDataFromExternalSource(path);
            data = Pre_Emphasis(data);
            double[,] data1 = new double[data.Length / 256- 1, 512];
            data1 = Frame_Blocking(data, data1.GetLength(0));
            data1 = HammingWindow_using(data1);
            double[] fft = new double[data1.GetLength(1)];
            double[] FourierResult = new double[data1.GetLength(0)];
            for (int i=0;i<FourierResult.Length;i++)
            {
                for (int j = 0; j < fft.Length; j++) fft[j] = data1[i, j];
                fft = FFT(fft);
                for (int j = 0; j < fft.Length; j++) FourierResult[i] += fft[j];
            }
            double Maxfreq = FourierResult.Max();
            double Minfreq = FourierResult.Min();
            double[] points = Filters(20, Minfreq, Maxfreq, data1.GetLength(1), 16000);
            double[,] filters = FilterBanks(points, data1.GetLength(0),20);
            FourierResult = LogEnrgSp(FourierResult, filters, 20, data1.GetLength(0));
            FourierResult = CosPreobr(FourierResult);
            return FourierResult;
        }
        double Srav(double[] mfcc, double[] mfcc1)
        {
            double sum = 0;
            for (int i = 0; i < mfcc.Length; i++)
            {
                sum += mfcc[i] - mfcc1[i];
            }
            sum = Math.Abs(sum);
            return sum;
        }
        double []Srav1(double[] mfcc,double[] mfcc1)
        {
            double[] result = new double[mfcc.Length];
            for (int i = 0; i < mfcc.Length; i++) result[i] = Math.Abs(mfcc[i] - mfcc1[i]);
            return result;
        }
        bool  Verification(double []result)
        {
            bool[] verpr = new bool[result.Length];
            bool verifn = false;
            int count = 0;
            for (int i=0;i<verpr.Length;i++)
            {
                if (result[i] < 2) count++;
            }
            if (count >= result.Length * 0.8) verifn = true;
            return verifn;
        }
        bool Verification(double result)
        {
            bool ver = false;
            if (result <= 3 && result >= 0) ver = true;
            return ver;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            string path = "Records\\6.wav";
            double[] mfcc = MFCC(path);
            string path1 = "Records\\2.wav";
            double[] mfcc1 = MFCC(path1);
           // double[] result = new double[mfcc.Length];
            //result = Srav1(mfcc, mfcc1);
           // for (int i = 0; i < result.Length; i++) textBox1.Text += Convert.ToString(result[i]) + " ; ";
            //textBox1.Text += "\r\n" + Convert.ToString(Verification(result));
            //for (int i = 31; i <= 35; i++)
            //{
            //    textBox1.Text += Convert.ToString(i) + "\r\n";
            //    for (int j = 31; j <= 35; j++)
            //    {

            //        path = "Records\\" + Convert.ToString(i) + ".wav";
            //        path1 = "Records\\" + Convert.ToString(j) + ".wav";
            //         mfcc = MFCC(path);
            //        mfcc1 = MFCC(path1);
            //        if (Verification(Srav(mfcc,mfcc1))) textBox1.Text += Convert.ToString(j)+"" + "TRUE;";
            //        else textBox1.Text += Convert.ToString(j) + "FALSE;";
            //    }
            //    textBox1.Text += "\r\n";
            //}
            //for (int i = 31; i <= 35; i++)
            //{
            //    textBox1.Text += Convert.ToString(i) + "\r\n";
            //    for (int j = 2; j <= 4; j++)
            //    {

            //        path = "Records\\" + Convert.ToString(i) + ".wav";
            //        path1 = "Records\\" + Convert.ToString(j) + ".wav";
            //        mfcc = MFCC(path);
            //        mfcc1 = MFCC(path1);
            //        if (Verification(Srav(mfcc, mfcc1))) textBox1.Text += Convert.ToString(j) + "" + "TRUE;";
            //        else textBox1.Text += Convert.ToString(j) + "FALSE;";
            //    }
            //    textBox1.Text += "\r\n";
            //}
            //for (int i = 31; i <= 35; i++)
            //{
            //    textBox1.Text += Convert.ToString(i) + "\r\n";
            //    for (int j = 23; j <= 25; j++)
            //    {

            //        path = "Records\\" + Convert.ToString(i) + ".wav";
            //        path1 = "Records\\" + Convert.ToString(j) + ".wav";
            //        mfcc = MFCC(path);
            //        mfcc1 = MFCC(path1);
            //        if (Verification(Srav(mfcc, mfcc1))) textBox1.Text += Convert.ToString(j) + "" + "TRUE;";
            //        else textBox1.Text += Convert.ToString(j) + "FALSE;";
            //    }
            //    textBox1.Text += "\r\n";
            //}
            //for (int i = 31; i <= 35; i++)
            //{
            //    textBox1.Text += Convert.ToString(i) + "\r\n";
            //    for (int j = 31; j <= 35; j++)
            //    {

            //        path = "Records\\" + Convert.ToString(i) + ".wav";
            //        path1 = "Records\\" + Convert.ToString(j) + ".wav";
            //        mfcc = MFCC(path);
            //        mfcc1 = MFCC(path1);
            //        if (Verification(Srav(mfcc, mfcc1))) textBox1.Text += Convert.ToString(j) + "" + "TRUE;";
            //        else textBox1.Text += Convert.ToString(j) + "FALSE;";
            //    }
            //    textBox1.Text += "\r\n";
            //}

            //if (Srav(mfcc, mfcc1) < 5 && Srav(mfcc, mfcc1) >= 0) textBox1.Text = "TRUE";
            //else textBox1.Text = "False";
            //for (int i = 0; i < mfcc.Length; i++)
            //{
            //    textBox1.Text += Convert.ToString(mfcc[i]) + "; ";
            //}
            //textBox1.Text += "||||||||";
            //for (int i = 0; i < mfcc.Length; i++)
            //{
            //    textBox1.Text += Convert.ToString(mfcc1[i]) + "; ";
            //}
            //textBox1.Text += "||||||||";
            //textBox1.Text += Convert.ToString(Srav(mfcc, mfcc1));
        }
    }
}
