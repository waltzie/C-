using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace WaltZie.Windows.Forms
{

    ///
    /// TextBox extension to mitigate keylkoggers
    ///

    // Issue Pending: caracters "[]" by sendkey
    // Issue Pending: multiple keypress. (switch to sync threads).
    // Issue Pending: Async call to handle unallowed chars

    public class PasswordTextBox : System.Windows.Forms.TextBox
    {
        public static PasswordTextBox Instance = new PasswordTextBox();

        private string junk = "";
        private uint _junkMax = 4;
        private uint _junkMin = 2;
        private byte _minchar = 32;
        private byte _maxchar = 126;
        private string _allowedchars="";
        private string _scramble;
        private bool _obfuscate = false;

        private static readonly Random random = new Random();
        private static readonly object syncLock = new object();
        private delegate string del(string s);

        public PasswordTextBox() {
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (String.IsNullOrEmpty(this.Text))
            {
                sendJunkChars();
            }
            base.OnMouseDown(e);
        }

        ///    
        /// The AllowedChars gets/sets the value of the string field, _allowedchars.
        ///

        [Category("Extensions"), Browsable(true), Description("String containig allowed chars")]
        public string AllowedChars
        {
            set
            {
               if ((value.Length) < 256) 
               { 
                   this._allowedchars = value; this._scramble = new string(_allowedchars.ToCharArray().OrderBy(x => Guid.NewGuid()).ToArray());
                }
            }
            get
            {
                return this._allowedchars;
            }
        }

        /// The Obfuscate gets/sets the value of the bool field, _obfuscate.
        ///
        [Category("Extensions"), Browsable(true), Description("Obfuscate input data")]
        public bool Obfuscate
        {
            set
            {
                this._obfuscate = value;
                if (this._obfuscate && _allowedchars == "") this.AllowedChars = " !\"#$%&'()*+-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
            }
            get
            {
                return this._obfuscate;
            }
        }

        
        /// The ClearText gets the value of the de-obfuscated string field, Text.
        /// Returns original de-obfuscated typed text.
        ///
        [Category("Extensions"), Browsable(true), Description("Clear text")]
        public string TextClear
        {
            get
            {
                return deobfuscate(this.Text);
            }
        }

        private bool IsAllowedChar(char c)
        {
            if (char.IsControl(c)) return false;
            if (this._allowedchars.Length > 0)
            {
                if (this._allowedchars.Contains(c.ToString())) return true;
                else return false;
            }
            else return true;
        }

        private string Randomchar()
        {
            return Convert.ToChar(random.Next(this._minchar, this._maxchar)).ToString();
        }

        private string junkGenerator()
        {
            string randomString = "";
            for (int i = 0; i < random.Next((int)this._junkMin, (int)this._junkMax); i++)
            randomString += Randomchar();
            return randomString;
        }
        private void sendJunkChars()
        {
            string outchar = "";
            lock (syncLock)
            {
                this.junk += junkGenerator();
                for (int i = 0; i < this.junk.Length; i++)
                if ("+^%~(){}".Contains(this.junk[i])) outchar += "{" + this.junk[i] + "}";
                else outchar += this.junk[i].ToString();
                this.Focus();
                SendKeys.Send(outchar);
            }
        }

        private char obfuscate(char s) {
            lock (syncLock)
            {
                if (this._obfuscate) return this._scramble[this._allowedchars.IndexOf(s)];
                else return s;
            }
        }
        private string deobfuscate(string s) {

            if (!this._obfuscate) return s;
            string outstr = "";
            for (int i=0; i<this.Text.Length;i++) 
            { 
                outstr += this._allowedchars[this._scramble.IndexOf(this.Text[i])].ToString(); 
            } 
            return outstr; 
        } 
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) { 
            if (this.junk.Length == 0) 
            { 
                if (keyData == Keys.Delete) OnKeyPress(new KeyPressEventArgs((Char)Keys.Back)); 
            } 
            return base.ProcessCmdKey(ref msg, keyData); 
        } 
        protected override void OnKeyPress(KeyPressEventArgs e) { // Controllare this.SelectedText 
            base.OnKeyPress(e); 
            string keyInput = e.KeyChar.ToString(); 
            if (this.junk.Length > 0)
            {
                //this char is junk
                e.Handled = true;
                lock(syncLock) this.junk = this.junk.Remove(this.junk.Length - 1);
            }
            else if (IsAllowedChar(e.KeyChar))
            {
                e.KeyChar = obfuscate(e.KeyChar);
                sendJunkChars();
            }
            else if (e.KeyChar == '\b')
            {
                try
                {
                // Backspace key is OK
                    sendJunkChars();
                }
                catch { e.Handled = true; }
            }
            else
            {
                // Consume this invalid key and beep
                e.Handled = true;
                System.Media.SystemSounds.Exclamation.Play();
            }
        }
    }
}
