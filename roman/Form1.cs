using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using NAudio.Wave;

namespace roman
{
    public partial class Form1 : Form
    {
        
        public string soundTrack = "src/sound/";
        public string soundTmpTrack = "src/tmp/";
        public string trackToSaveVoice = "";
        Voice core = null;
        Message message = new Message();
        Mixer mixer = new Mixer();
        
        public Form1()
        {
            InitializeComponent();
            labelStatus.Text = message.searchAvailableVoice;
            DirectoryInfo d = new DirectoryInfo("src/db/");
            foreach (FileInfo file in d.GetFiles())
            {
                if (file.Extension == ".db")
                {
                    comboBox1.Items.Add(Path.GetFileNameWithoutExtension(file.Name));
                }
            }
            labelStatus.Text = message.selectVoice;

            /*using (System.IO.StreamWriter file = new System.IO.StreamWriter("src/mleko.txt", true))
            {
                //file.WriteLine("-------------------------------");
            }*/
        }

        private void btnRead_Click(object sender, EventArgs e)
        {
            if (!isSetCore()) return;
            if (textArea.Text == "") return;

            labelStatus.Text = message.generateVoice;

            setMixer();

            core.readText(textArea.Text, mixer);

            if (!core.ok)
            {
                labelStatus.Text = message.empty;
                return;
            }

            labelStatus.Text = message.reading;

            enableSoundPanel(true);
            core.romanPlay();
            core.waveOutDevice.PlaybackStopped += delegate { enableSoundPanel(false); };

            labelStatus.Text = message.empty;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Application.DoEvents();
            string text = comboBox1.SelectedItem.ToString().ToLower();
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                core = new Voice(text);
            }).Start();
            
            labelStatus.Text = message.insertText;
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            if (!isSetCore()) return;
            try
            {
                core.waveOutDevice.Pause();
            }
            catch
            {
                MessageBox.Show(message.selectVoice);
            }
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (!isSetCore()) return;
            try
            {
                core.waveOutDevice.Play();
            }
            catch
            {
                MessageBox.Show(message.selectVoice);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (!isSetCore()) return;
            try
            {
                core.waveOutDevice.Stop();
            }
            catch
            {
                MessageBox.Show(message.selectVoice);
            }
        }

        private bool isSetCore()
        {
            if (core == null)
            {
                MessageBox.Show(message.selectVoice);
                return false;
            }
            else return true;
        }

        private void enableSoundPanel(bool state)
        {
            btnPause.Enabled = state;
            btnPlay.Enabled = state;
            btnStop.Enabled = state;

            btnRead.Enabled = !state;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!isSetCore()) return;
            if (trackToSaveVoice == "")
            {
                saveVoice.ShowDialog();
            }
            setMixer();
            core.saveVoiceToFile(textArea.Text, trackToSaveVoice, mixer);
        }

        private void saveVoice_FileOk(object sender, CancelEventArgs e)
        {
            trackToSaveVoice = saveVoice.FileName;
        }

        private void setMixer()
        {
            mixer.lvlLoud = lvlLoud.Value;
        }

        private void checkEffectPrzester_CheckedChanged(object sender, EventArgs e)
        {
            mixer.przester = checkEffectPrzester.Checked;
        }
    }
}
