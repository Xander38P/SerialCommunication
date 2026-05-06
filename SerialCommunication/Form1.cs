using System;
using System.Linq;
using System.IO.Ports;
using System.Windows.Forms;

namespace SerialCommunication
{
    public partial class Form1 : Form
    {
        private SerialPort serialPortArduino;

        public Form1()
        {
            InitializeComponent();

            serialPortArduino = new SerialPort();
            serialPortArduino.ReadTimeout = 1000;
            serialPortArduino.WriteTimeout = 1000;
            serialPortArduino.NewLine = "\n";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                string[] portNames = SerialPort.GetPortNames().Distinct().ToArray();
                comboBoxPoort.Items.Clear();
                comboBoxPoort.Items.AddRange(portNames);
                if (comboBoxPoort.Items.Count > 0) comboBoxPoort.SelectedIndex = 0;

                comboBoxBaudrate.SelectedIndex = comboBoxBaudrate.Items.IndexOf("115200");
            }
            catch (Exception) { }
        }

        private void cboPoort_DropDown(object sender, EventArgs e)
        {
            try
            {
                string selected = (string)comboBoxPoort.SelectedItem;
                string[] portNames = SerialPort.GetPortNames().Distinct().ToArray();

                comboBoxPoort.Items.Clear();
                comboBoxPoort.Items.AddRange(portNames);

                comboBoxPoort.SelectedIndex = comboBoxPoort.Items.IndexOf(selected);
            }
            catch (Exception)
            {
                if (comboBoxPoort.Items.Count > 0) comboBoxPoort.SelectedIndex = 0;
            }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            try
            {
                // Is er al een verbinding?
                if (serialPortArduino.IsOpen)
                {
                    // === VERBINDING VERBREKEN ===

                    // 1. Verbreek de verbinding m.b.v. de functie Close
                    serialPortArduino.Close();

                    // 2. Vink de radiobutton radiobuttonVerbonden af
                    radioButtonVerbonden.Checked = false;

                    // 3. Verander de tekst op buttonConnect in Connect
                    buttonConnect.Text = "Connect";

                    // 4. Pas het statuslabel aan
                    labelStatus.Text = "Verbinding verbroken";
                }
                else
                {
                    // === VERBINDING MAKEN ===

                    if (comboBoxPoort.SelectedItem == null)
                    {
                        MessageBox.Show("Selecteer eerst een poort.", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Zet de properties
                    serialPortArduino.PortName = comboBoxPoort.SelectedItem.ToString();

                    int baudRate;
                    int.TryParse(comboBoxBaudrate.SelectedItem?.ToString() ?? "115200", out baudRate);
                    serialPortArduino.BaudRate = baudRate;

                    serialPortArduino.DataBits = (int)numericUpDownDatabits.Value;

                    if (radioButtonParityEven.Checked) serialPortArduino.Parity = Parity.Even;
                    else if (radioButtonParityOdd.Checked) serialPortArduino.Parity = Parity.Odd;
                    else if (radioButtonParityMark.Checked) serialPortArduino.Parity = Parity.Mark;
                    else if (radioButtonParitySpace.Checked) serialPortArduino.Parity = Parity.Space;
                    else serialPortArduino.Parity = Parity.None;

                    if (radioButtonStopbitsNone.Checked) serialPortArduino.StopBits = StopBits.None;
                    else if (radioButtonStopbitsOne.Checked) serialPortArduino.StopBits = StopBits.One;
                    else if (radioButtonStopbitsOnePointFive.Checked) serialPortArduino.StopBits = StopBits.OnePointFive;
                    else if (radioButtonStopbitsTwo.Checked) serialPortArduino.StopBits = StopBits.Two;
                    else serialPortArduino.StopBits = StopBits.One;

                    if (radioButtonHandshakeNone.Checked) serialPortArduino.Handshake = Handshake.None;
                    else if (radioButtonHandshakeRTS.Checked) serialPortArduino.Handshake = Handshake.RequestToSend;
                    else if (radioButtonHandshakeXonXoff.Checked) serialPortArduino.Handshake = Handshake.XOnXOff;
                    else if (radioButtonHandshakeRTSXonXoff.Checked) serialPortArduino.Handshake = Handshake.RequestToSendXOnXOff;
                    else serialPortArduino.Handshake = Handshake.None;

                    serialPortArduino.DtrEnable = checkBoxDtrEnable.Checked;
                    serialPortArduino.RtsEnable = checkBoxRtsEnable.Checked;

                    // Maak een verbinding m.b.v. de functie Open
                    serialPortArduino.Open();

                    // Wacht op de Arduino en maak buffers leeg 
                    // (Aangepast: Sleep ingeschakeld met 2000ms zodat de Arduino kan opstarten)
                    System.Threading.Thread.Sleep(2000);
                    try { serialPortArduino.DiscardInBuffer(); } catch { }
                    try { serialPortArduino.DiscardOutBuffer(); } catch { }

                    // Verzend het woord ping naar de arduino
                    serialPortArduino.WriteLine("ping");

                    string response = "";
                    bool isPongReceived = false;

                    // Controleer het antwoord, dit moet pong zijn
                    try
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            response = serialPortArduino.ReadLine().Trim();
                            if (response == "pong")
                            {
                                isPongReceived = true;
                                break;
                            }
                        }
                    }
                    catch (TimeoutException) { }

                    if (isPongReceived)
                    {
                        // Vink de radiobutton aan, verander tekst in Disconnect, pas statuslabel aan
                        radioButtonVerbonden.Checked = true;
                        buttonConnect.Text = "Disconnect";
                        labelStatus.Text = $"Verbonden met {serialPortArduino.PortName} @ {serialPortArduino.BaudRate}";
                    }
                    else
                    {
                        serialPortArduino.Close();
                        MessageBox.Show($"Handshake mislukt. Geen geldig antwoord ontvangen.", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Er is een fout opgetreden: {ex.Message}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (serialPortArduino.IsOpen) serialPortArduino.Close();
                radioButtonVerbonden.Checked = false;
                buttonConnect.Text = "Connect";
                labelStatus.Text = "Error";
            }
        }

        // === NIEUWE EVENTHANDLER HIER TOEGEVOEGD ===
        private void checkBoxDigital2_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                // 1. Controleer of er een open seriële verbinding is
                if (serialPortArduino != null && serialPortArduino.IsOpen)
                {
                    // 2 & 3. Controleer de status en maak de commandostring 
                    string commando = checkBoxDigital2.Checked ? "set d2 high" : "set d2 low";

                    // 4. Verstuur de commandostring
                    serialPortArduino.WriteLine(commando);
                }
                else
                {
                    MessageBox.Show("Kan het commando niet verzenden: de seriële poort is niet geopend.", "Geen verbinding", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            // 5. Vang eventuele fouten af
            catch (Exception ex)
            {
                MessageBox.Show($"Er is een fout opgetreden tijdens het communiceren met de Arduino:\n\n{ex.Message}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void checkBoxDigital2_CheckedChanged_1(object sender, EventArgs e)
        {
            try
            {
                // 1. Controleer of er een open seriële verbinding is
                if (serialPortArduino != null && serialPortArduino.IsOpen)
                {
                    // 2 & 3. Controleer de status en maak de commandostring 
                    string commando = checkBoxDigital2.Checked ? "set d2 high" : "set d2 low";

                    // 4. Verstuur de commandostring
                    serialPortArduino.WriteLine(commando);
                }
                else
                {
                    MessageBox.Show("Kan het commando niet verzenden: de seriële poort is niet geopend.", "Geen verbinding", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            // 5. Vang eventuele fouten af
            catch (Exception ex)
            {
                MessageBox.Show($"Er is een fout opgetreden tijdens het communiceren met de Arduino:\n\n{ex.Message}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void checkBoxDigital3_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                // 1. Controleer of er een open seriële verbinding is
                if (serialPortArduino != null && serialPortArduino.IsOpen)
                {
                    // 2 & 3. Controleer de status en maak de commandostring 
                    string commando = checkBoxDigital3.Checked ? "set d3 high" : "set d3 low";

                    // 4. Verstuur de commandostring
                    serialPortArduino.WriteLine(commando);
                }
                else
                {
                    MessageBox.Show("Kan het commando niet verzenden: de seriële poort is niet geopend.", "Geen verbinding", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            // 5. Vang eventuele fouten af
            catch (Exception ex)
            {
                MessageBox.Show($"Er is een fout opgetreden tijdens het communiceren met de Arduino:\n\n{ex.Message}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void checkBoxDigital4_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                // 1. Controleer of er een open seriële verbinding is
                if (serialPortArduino != null && serialPortArduino.IsOpen)
                {
                    // 2 & 3. Controleer de status en maak de commandostring 
                    string commando = checkBoxDigital4.Checked ? "set d4 high" : "set d4 low";

                    // 4. Verstuur de commandostring
                    serialPortArduino.WriteLine(commando);
                }
                else
                {
                    MessageBox.Show("Kan het commando niet verzenden: de seriële poort is niet geopend.", "Geen verbinding", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            // 5. Vang eventuele fouten af
            catch (Exception ex)
            {
                MessageBox.Show($"Er is een fout opgetreden tijdens het communiceren met de Arduino:\n\n{ex.Message}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void trackBarPWM9_Scroll(object sender, EventArgs e)
        {
            try
            {
                // 1. Controleer of er een open seriële verbinding is
                if (serialPortArduino != null && serialPortArduino.IsOpen)
                {
                    // 2. Haal de waarde op en maak de commandostring.
                    // Met het $-teken (string interpolation) kunnen we de waarde direct in de tekst plakken.
                    string commando = $"set pwm9 {trackBarPWM9.Value}";

                    // 3. Verstuur de commandostring
                    serialPortArduino.WriteLine(commando);
                }
            }
            // 4. Vang eventuele fouten af
            catch (Exception ex)
            {
                // Toon een foutmelding als er iets misgaat met de communicatie
                MessageBox.Show($"Er is een fout opgetreden tijdens het instellen van de PWM waarde:\n\n{ex.Message}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void trackBarPWM10_Scroll(object sender, EventArgs e)
        {
            try
            {
                // 1. Controleer of er een open seriële verbinding is
                if (serialPortArduino != null && serialPortArduino.IsOpen)
                {
                    // 2. Haal de waarde op en maak de commandostring.
                    // Met het $-teken (string interpolation) kunnen we de waarde direct in de tekst plakken.
                    string commando = $"set pwm10 {trackBarPWM10.Value}";

                    // 3. Verstuur de commandostring
                    serialPortArduino.WriteLine(commando);
                }
            }
            // 4. Vang eventuele fouten af
            catch (Exception ex)
            {
                // Toon een foutmelding als er iets misgaat met de communicatie
                MessageBox.Show($"Er is een fout opgetreden tijdens het instellen van de PWM waarde:\n\n{ex.Message}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void trackBarPWM11_Scroll(object sender, EventArgs e)
        {
            try
            {
                // 1. Controleer of er een open seriële verbinding is
                if (serialPortArduino != null && serialPortArduino.IsOpen)
                {
                    // 2. Haal de waarde op en maak de commandostring.
                    // Met het $-teken (string interpolation) kunnen we de waarde direct in de tekst plakken.
                    string commando = $"set pwm11 {trackBarPWM11.Value}";

                    // 3. Verstuur de commandostring
                    serialPortArduino.WriteLine(commando);
                }
            }
            // 4. Vang eventuele fouten af
            catch (Exception ex)
            {
                // Toon een foutmelding als er iets misgaat met de communicatie
                MessageBox.Show($"Er is een fout opgetreden tijdens het instellen van de PWM waarde:\n\n{ex.Message}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}