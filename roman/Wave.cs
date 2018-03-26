using System.IO;

namespace roman
{
    class Wave
    {
        public int length = 0;
        public short channels = 0;
        public int samplerate = 0;
        public int DataLength = 0;
        public short BitsPerSample = 0;

        public void WaveHeaderIN(FileStream file)
        {
            BinaryReader br = new BinaryReader(file);
            length = (int)file.Length - 8;
            file.Position = 22;
            channels = br.ReadInt16();
            file.Position = 24;
            samplerate = br.ReadInt32();
            file.Position = 34;

            BitsPerSample = br.ReadInt16();
            DataLength = (int)file.Length - 44;
            br = null;
        }

        public void addWave(Wave wave)
        {
            this.DataLength += wave.DataLength;
            this.length += wave.length;
        }

        public void WaveHeaderOUT(FileStream fileOutput, BinaryWriter binaryWriter)
        {
            fileOutput.Position = 0;
            binaryWriter.Write(new char[4] { 'R', 'I', 'F', 'F' });
            binaryWriter.Write(length);
            binaryWriter.Write(new char[8] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            binaryWriter.Write((int)16);
            binaryWriter.Write((short)1);
            binaryWriter.Write(channels);
            binaryWriter.Write(samplerate);
            binaryWriter.Write((int)(samplerate * ((BitsPerSample * channels) / 8)));
            binaryWriter.Write((short)((BitsPerSample * channels) / 8));
            binaryWriter.Write(BitsPerSample);
            binaryWriter.Write(new char[4] { 'd', 'a', 't', 'a' });
            binaryWriter.Write(DataLength);
        }

    }
}
