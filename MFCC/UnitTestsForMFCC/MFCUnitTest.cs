using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MFCC;
using NAudio.Wave;
using NAudio.Dsp;

namespace UnitTestsForMFCC
{
    [TestClass]
    public class MFCUnitTest
    {
        /// <summary>
        /// Пример unit=теста. Перед ним стоит атрибут [TestMethod]
        /// </summary>
        [TestMethod]
        public void ReadWavTest()
        {
            // Создали объект, который тестируем
            MainForm mf = new MainForm();

            // прменили тестируемый метод - ReadDataFromExternalSource(inPath)
            string inPath = "1.wav";
            double [] readAsBinaryData = mf.ReadDataFromExternalSource(inPath);
            // сделали проверку. Пока просто чтоданные найдены 
            Assert.AreEqual(true, readAsBinaryData.Length > 0, "Выбор файла данных");

            // Прочли тот же файл стандартной бибилотекой 
            float[] readByNAudio = null;
            int r = 0;
            using (var reader = new AudioFileReader(inPath))
            {
                readByNAudio = new float[reader.Length/sizeof(float)];
                r = reader.Read(readByNAudio, 0, readByNAudio.Length);
            }
            //Проверили, что прочтено данных столько же как и проверяемым методом
            Assert.AreEqual(r, readAsBinaryData.Length, "Фактическое Количество данных");
            // Вызвали метод сравнения двух массивов. Значения в них отличаются множителем 32768.
            // Если метод вернет количество элементов массива, то все ОК. Если нет, то найдено существенное различие!
            Assert.AreEqual(r, copareFloatAndDouble(readByNAudio, readAsBinaryData, 32768, 0.0000000001), "Эквивалентность данных");
        }

        /// <summary>
        /// Метод сравнения 2-х массивов. 
        /// Это не тест! У него нет атрибута [TestMethod] и есть параметры и возвращаемое значение
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="scale">множитель, которыми они отличаются</param>
        /// <param name="accuracy">точность сравнения</param>
        /// <returns>Длину массива, если они эжквивалентны.
        /// Номер элемента, на котором найдено различе или -1, если длина массивов разная</returns>
        static int copareFloatAndDouble(float[] a, double[] b, double scale, double accuracy)
        {
            if (a.Length != b.Length)
                return -1;
            for (int i = 0; i < a.Length; i++)
                if (Math.Abs(a[i] - b[i]/scale) > accuracy)
                    return i;
            return a.Length;
        }

        /// <summary>
        /// Правильная функция для вычисления оконной функции Хамминга.
        /// </summary>
        /// <param name="n">Номер канала (0 до N-1)</param>
        /// <param name="N">Общее количество каналов</param>
        /// <returns></returns>
        public static double Hamming(int n, int N)
        {
            double alpha = 0.53836;
            return alpha - (1 - alpha) * Math.Cos(2 * Math.PI * n / (N - 1));
        }

        /// <summary>
        /// Здесь должно быть тестирование правильности вычисления фкнуции Хамминга
        /// Выше приведена правильная реализация.
        /// Нужно написать тест, кторый проверит, что в MainForm функция HammingWindow эквивалентна правильной.
        /// </summary>
        [TestMethod]
        public void HammingTest()
        {
        }
        /// <summary>
        /// Здесь должно быть тестирование правильности вычислениф быстрого преобразования Фурье в MainForm
        /// В классе FFT реализовано правильное преобразование.
        /// Нужно написать тест, кторый проверит, что в MainForm реализация FFT эквивалентна правильной.
        /// В методе приводится пример использования класса FFT
        /// </summary>
        [TestMethod]
        public void FFTTest()
        {

            FFT fft = new FFT();

            // Будем преобразовывать массивы длиной 2^9 = 512
            int N = 512;
            Complex []sin = new Complex[N];
            
            double a = 2; // Частота 
            for (int i = 0; i < 512; i++)
                sin[i].X = (float)Math.Sin(i*2*a*Math.PI/N);
            
            // Прямое преобразование
            fft.Fft(9, sin);
            // Обратное преобразование
            fft.Fft(9, sin, true);

            
        }
    }
}
