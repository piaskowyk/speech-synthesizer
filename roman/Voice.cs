using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using NAudio.Wave;
using System.Diagnostics;

namespace roman
{
    class Voice
    {
        public Dictionary<string, byte[]> fileByte = new Dictionary<string, byte[]>();
        public Dictionary<string, FileStream> fileStream = new Dictionary<string, FileStream>();
        public Dictionary<string, long> lenghtArrayByte = new Dictionary<string, long>();
        public string soundTrack = "src/sound/";
        public string soundTmpTrack = "src/tmp/";
        public string fileOuptutWavName = "tmp.wav";
        public string fileMix = "tmpMix.wav";
        public string fileOutputMP3Name = "out.mp3";
        public string name = "";
        public bool isNull = false;
        public IWavePlayer waveOutDevice = new WaveOut();
        public bool ok = false;

        private int kbpsInMp3 = 128;

        public Voice(string n)
        {
            name = n;
            SQLiteConnection db = new SQLiteConnection("Data Source=src/db/"+ name + ".db");
            db.Open();
            SQLiteCommand cmd = new SQLiteCommand(null, db);
            cmd.CommandText = "select * from element";
            SQLiteDataReader result = cmd.ExecuteReader();
            string path = soundTrack + name + "/";
            string strPointer = "";
            long lentghSingleArrayByte = 0;
            while (result.Read())
            {
                strPointer = result["char"].ToString();
                fileStream[strPointer] = new FileStream(
                    path + result["track"], 
                    FileMode.Open, 
                    FileAccess.Read
                    );
                
                lentghSingleArrayByte = fileStream[strPointer].Length - 44;

                if (lentghSingleArrayByte < 0) lentghSingleArrayByte = 0;

                byte[] arrfile = new byte[lentghSingleArrayByte];
                fileStream[strPointer].Position = 44;
                fileStream[strPointer].Read(arrfile, 0, arrfile.Length);
                fileByte[strPointer] = arrfile;

                lenghtArrayByte[strPointer] = lentghSingleArrayByte;
            }

        }

        private bool isNum(string num)
        {
            string[] allNum = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            foreach(string item in allNum)
            {
                if (num == item) return true;
            }
            return false;
        }

        public void readText(string textArea, Mixer mixer)
        {
            ok = false;
            List<string> items = sliceText(textArea);
            long lenghtOutputByteArray = calculateTabSize(items);
            if (lenghtOutputByteArray == 0) return;
            saveToFile(lenghtOutputByteArray, items, soundTmpTrack + fileOutputMP3Name, mixer);
            ok = true;
        }

        public void saveVoiceToFile(string textArea, string path, Mixer mixer)
        {
            List<string> items = sliceText(textArea);
            long lenghtOutputByteArray = calculateTabSize(items);
            if (lenghtOutputByteArray == 0) return;

            saveToFile(lenghtOutputByteArray, items, path + ".mp3", mixer);
            File.Delete(soundTmpTrack + fileOuptutWavName);
        }

        public List<string> sliceText(string textArea)
        {
            List<string> items = new List<string>();

            string item = null;
            string itemTmp = null;
            string strNum = null;
            int intFromStr = 0;
            int step = 0;
            string allText = textArea.ToString();
            for (int i = 0; i < textArea.Length; i++)
            {
                item = null;
                itemTmp = allText[i].ToString();
                step = 0;

                if (isNum(itemTmp))//if ti is number
                {

                    strNum = itemTmp;
                    step++;
                    while (i + step < textArea.Length && isNum(allText[i + step].ToString()))
                    {
                        strNum += allText[i + step].ToString();
                        step++;
                    }
                    if (step > 1) i += step - 1;

                    string copyToRevers = "";
                    for (int k = strNum.Length - 1; k >= 0; k--)
                    {
                        copyToRevers += strNum[k];
                    }
                    strNum = copyToRevers;

                    int lenNum = strNum.Length;
                    bool isBig = false;
                    if (lenNum > 6) isBig = true;
                    else intFromStr = Convert.ToInt32(strNum);

                    if (isBig || intFromStr >= 1000000)
                    {
                        //czytaj po cyferce
                        copyToRevers = "";
                        for (int k = strNum.Length - 1; k >= 0; k--)
                        {
                            copyToRevers += strNum[k];
                        }
                        strNum = copyToRevers;

                        foreach (char numItem in strNum)
                        {
                            if (fileStream.ContainsKey(numItem.ToString()))
                            {
                                items.Add(numItem.ToString());
                            }
                        }
                        continue;
                    }

                    //if intFromStr < 1000000
                    int[] numberOnPisition = new int[7];
                    bool[] numberIsOnPisition = new bool[7];
                    string tmpNumberStr;
                    int chunkNumber;

                    for (int k = 0; k < 7; k++)
                    {
                        numberIsOnPisition[k] = false;
                    }

                    if (lenNum == 6)
                    {
                        tmpNumberStr = strNum[5].ToString() + "00";
                        numberIsOnPisition[6] = true;
                        if (fileStream.ContainsKey(tmpNumberStr))
                        {
                            items.Add(tmpNumberStr);
                        }
                    }

                    if (lenNum >= 5)
                    {
                        tmpNumberStr = strNum[4].ToString() + strNum[3].ToString();
                        chunkNumber = Convert.ToInt16(tmpNumberStr);
                        if (strNum[4].ToString() != "0")//if deceminal num is different 0
                        {
                            numberIsOnPisition[5] = true;
                            numberIsOnPisition[4] = true;
                        }

                        if (chunkNumber > 10 && chunkNumber <= 19)
                        {
                            if (fileStream.ContainsKey(tmpNumberStr))
                            {
                                items.Add(tmpNumberStr);
                            }
                        }

                        //10, 20, 30 ...
                        else if (chunkNumber % 10 == 0)
                        {
                            if (fileStream.ContainsKey(tmpNumberStr))
                            {
                                items.Add(tmpNumberStr);
                            }
                        }

                        else
                        {
                            // 21-29, 31-39...
                            numberOnPisition[2] = (chunkNumber / 10) * 10;
                            numberOnPisition[1] = chunkNumber % 10;

                            tmpNumberStr = numberOnPisition[2].ToString();
                            if (fileStream.ContainsKey(tmpNumberStr))
                            {
                                items.Add(tmpNumberStr);
                            }

                            if (numberOnPisition[1] != 0)
                            {
                                tmpNumberStr = numberOnPisition[1].ToString();
                                if (fileStream.ContainsKey(tmpNumberStr))
                                {
                                    items.Add(tmpNumberStr);
                                }
                            }
                        }
                    }

                    if (lenNum >= 4)
                    {
                        tmpNumberStr = strNum[3].ToString();
                        chunkNumber = Convert.ToInt16(tmpNumberStr);
                        if (numberIsOnPisition[5] == false)
                        {
                            if (tmpNumberStr != "1" && fileStream.ContainsKey(tmpNumberStr))
                            {
                                items.Add(tmpNumberStr);
                            }
                            numberIsOnPisition[4] = true;
                        }

                        if (numberIsOnPisition[6] == true || numberIsOnPisition[5] == true)
                        {
                            if (tmpNumberStr == "1")
                            {
                                //tysięcy
                                if (fileStream.ContainsKey("tysięcy"))
                                {
                                    items.Add("tysięcy");
                                }
                            }
                            else if (tmpNumberStr == "2" || tmpNumberStr == "3")
                            {
                                //tysięce
                                if (fileStream.ContainsKey("tysiące"))
                                {
                                    items.Add("tysiące");
                                }
                            }
                            else
                            {
                                //tysięcy
                                if (fileStream.ContainsKey("tysięcy"))
                                {
                                    items.Add("tysięcy");
                                }
                            }
                        }
                        else
                        {
                            if (tmpNumberStr == "1")
                            {
                                //tysiąc
                                if (fileStream.ContainsKey("tysiąc"))
                                {
                                    items.Add("tysiąc");
                                }
                            }
                            else if (tmpNumberStr == "2" || tmpNumberStr == "3")
                            {
                                //tysięce
                                if (fileStream.ContainsKey("tysięce"))
                                {
                                    items.Add("tysięce");
                                }
                            }
                            else
                            {
                                //tysięcy
                                if (fileStream.ContainsKey("tysięcy"))
                                {
                                    items.Add("tysięcy");
                                }
                            }
                        }
                    }

                    if (lenNum >= 3)
                    {
                        tmpNumberStr = strNum[2].ToString() + "00";
                        numberIsOnPisition[2] = true;
                        if (fileStream.ContainsKey(tmpNumberStr))
                        {
                            items.Add(tmpNumberStr);
                        }
                    }

                    if (lenNum >= 2)
                    {
                        tmpNumberStr = strNum[1].ToString() + strNum[0].ToString();
                        chunkNumber = Convert.ToInt16(tmpNumberStr);
                        if (strNum[1].ToString() != "0")//if deceminal num is different 0
                        {
                            numberIsOnPisition[2] = true;
                            numberIsOnPisition[1] = true;
                        }

                        if (chunkNumber > 10 && chunkNumber <= 19)
                        {
                            if (fileStream.ContainsKey(tmpNumberStr))
                            {
                                items.Add(tmpNumberStr);
                            }
                        }

                        //10, 20, 30 ...
                        else if (chunkNumber % 10 == 0)
                        {
                            if (fileStream.ContainsKey(tmpNumberStr))
                            {
                                items.Add(tmpNumberStr);
                            }
                        }

                        else
                        {
                            // 21-29, 31-39...
                            numberOnPisition[2] = (chunkNumber / 10) * 10;
                            numberOnPisition[1] = chunkNumber % 10;

                            tmpNumberStr = numberOnPisition[2].ToString();
                            if (fileStream.ContainsKey(tmpNumberStr))
                            {
                                items.Add(tmpNumberStr);
                            }

                            if (numberOnPisition[1] != 0)
                            {
                                tmpNumberStr = numberOnPisition[1].ToString();
                                if (fileStream.ContainsKey(tmpNumberStr))
                                {
                                    items.Add(tmpNumberStr);
                                }
                            }
                        }
                    }

                    if (lenNum >= 1)
                    {
                        if (numberIsOnPisition[1] == false && fileStream.ContainsKey(strNum))
                        {
                            items.Add(strNum);
                        }
                    }

                }
                else //if this word
                {
                    while (fileStream.ContainsKey(itemTmp))
                    {
                        item = itemTmp;
                        step++;
                        if (i + step < textArea.Length)
                        {
                            itemTmp += textArea[i + step].ToString();
                        }
                        else break;
                    }

                    if (step > 1) i += step - 1;
                    if (item == null || item == " ") continue;
                    items.Add(item);
                }
            }

            return items;
        }

        public long calculateTabSize(List<string> items)
        {
            long size = 0;
            foreach (string item in items)
            {
                size += lenghtArrayByte[item];
            }
            return size;
        }

        public void saveToFile(long lenghtOutputByteArray, List<string> items, string path, Mixer mixer)
        {
            Wave outputWave = new Wave();
            Wave chunkWave = new Wave();

            byte[] outputArray = new byte[lenghtOutputByteArray];
            long iterator = 0;
            foreach (string chunk in items)
            {
                //get patametest from file to var in object
                chunkWave.WaveHeaderIN(fileStream[chunk]);
                outputWave.addWave(chunkWave);

                for (int i = 0; i < lenghtArrayByte[chunk]; i++)
                {
                    outputArray[iterator] = fileByte[chunk][i];
                    iterator++;
                }
            }

            if (items.Count <= 0) return;

            //set headers in output file from first item
            chunkWave.WaveHeaderIN(fileStream[items[0]]);

            FileStream outputFile;

            if (!File.Exists(soundTmpTrack + fileOuptutWavName))
            {
                outputFile = new FileStream(soundTmpTrack + fileOuptutWavName, FileMode.Create);
                outputFile.Close();
            }
            outputFile = new FileStream(soundTmpTrack + fileOuptutWavName, FileMode.Open, FileAccess.Write);
            BinaryWriter binaryWriter = new BinaryWriter(outputFile);

            outputWave.BitsPerSample = chunkWave.BitsPerSample;
            outputWave.channels = chunkWave.channels;
            outputWave.samplerate = chunkWave.samplerate;
            outputWave.WaveHeaderOUT(outputFile, binaryWriter);

            outputFile.Close();
            binaryWriter.Close();

            //save raw data to output file
            outputFile = new FileStream(soundTmpTrack + fileOuptutWavName, FileMode.Append, FileAccess.Write);
            binaryWriter = new BinaryWriter(outputFile);

            binaryWriter.Write(outputArray);


            // close output file
            binaryWriter.Close();
            outputFile.Close();

            binaryWriter = null;
            outputFile = null;
            outputArray = null;
            GC.Collect();

            bool isMixed = mixer.makeLoud(soundTmpTrack + fileOuptutWavName, soundTmpTrack + fileMix);

            //convert wave to mp3
            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            if (isMixed)
            {
                cmd.StandardInput.WriteLine(
                "lame -b " + kbpsInMp3.ToString() + " " + soundTmpTrack + fileMix + " " + path
                );
            }
            else
            {
                cmd.StandardInput.WriteLine(
                "lame -b " + kbpsInMp3.ToString() + " " + soundTmpTrack + fileOuptutWavName + " " + path
                );
            }
            
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();
        }

        public void romanPlay()
        {
            //FileStream fs = new FileStream(soundTmpTrack + "out.wav", FileMode.Open, FileAccess.Read);
            //System.Media.SoundPlayer sp = new System.Media.SoundPlayer(fs);
            //sp.Play();
            //fs.Close();

            if (!ok) return;

            Mp3FileReader audioFileReader = new Mp3FileReader(soundTmpTrack + fileOutputMP3Name);
            waveOutDevice = new WaveOut();
            waveOutDevice.Init(audioFileReader);
            waveOutDevice.Play();
            waveOutDevice.PlaybackStopped += delegate { endPlay(audioFileReader); };
        }

        void endPlay(Mp3FileReader sender)
        {
            sender.Close();
            File.Delete(soundTmpTrack + fileOutputMP3Name);
            File.Delete(soundTmpTrack + fileOuptutWavName);
            File.Delete(soundTmpTrack + fileMix);
        }

    }
}
