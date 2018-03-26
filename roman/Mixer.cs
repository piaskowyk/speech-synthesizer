using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace roman
{
    class Mixer
    {
        public int lvlLoud = 0;
        public bool przester = false;

        public bool makeLoud(string inPath, string outPath)
        {

            if (lvlLoud == 50 && !this.przester)
            {
                return false;
            }

            float max = 0;

            AudioFileReader reader = new AudioFileReader(inPath);
            
            // find the max peak
            float[] buffer = new float[reader.WaveFormat.SampleRate];
            int read;
            do
            {
                read = reader.Read(buffer, 0, buffer.Length);
                for (int n = 0; n < read; n++)
                {
                    var abs = Math.Abs(buffer[n]);
                    if (abs > max) max = abs;
                }
            } while (read > 0);

            if (max == 0 || max > 1.0f)
                throw new InvalidOperationException("File cannot be normalized");

            // rewind and amplify
            reader.Position = 0;
            if (this.przester)
            {
                reader.Volume = 1.0f / (max * (lvlLoud / 100));
            }
            else
            {
                reader.Volume = 1.0f / (max * (100 / (float)lvlLoud));
            }

            // write out to a new WAV file
            WaveFileWriter.CreateWaveFile16(outPath, reader);
            reader.Close();
            
            return true;
        }

    }
}
