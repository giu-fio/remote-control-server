using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ServerWPF
{
    public class MyClibpoard
    {

        private byte[] COPY_EFFECT = new byte[] { 5, 0, 0, 0 };
        private byte[] CUT_EFFECT = new byte[] { 2, 0, 0, 0 };

        public bool IsAudio { get; set; }

        public bool IsImage { get; set; }

        public bool IsFile { get; set; }

        public bool IsText { get; set; }

        public bool isEmpty { get; set; }

        private StringCollection files;
        private byte[] img;
        private String txt;
        private Stream aux;

        public StringCollection FileDropList { get { return files; } set { if (value != null) { isEmpty = false; IsFile = true; files = value; } } }

        public byte[] Image { get { return img; } set { if (value != null) { isEmpty = false; IsImage = true; img = value; } } }

        public Stream Audio { get { return aux; } set { if (value != null) { isEmpty = false; IsAudio = true; aux = value; } } }

        public String Text { get { return txt; } set { if (value != null) { isEmpty = false; IsText = true; txt = value; } } }

        public MyClibpoard()
        {
            IsAudio = false;
            IsImage = false;
            IsFile = false;
            IsText = false;
            isEmpty = true;
            FileDropList = null;
            Audio = null;
            Image = null;
            Text = null;
        }

        public void AcquireClipboardContent()
        {
            try
            {
                if (Clipboard.ContainsAudio())
                {
                    Audio = Clipboard.GetAudioStream();
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    FileDropList = Clipboard.GetFileDropList();
                }
                else if (Clipboard.ContainsImage())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        BitmapSource img = Clipboard.GetImage();
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(img));
                        encoder.Save(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        Image = ms.ToArray();
                    }
                }
                else if (Clipboard.ContainsText())
                {
                    Text = Clipboard.GetText();
                }
                else
                {
                    isEmpty = true;
                }
            }
            catch (Exception ex) { Trace.TraceError("Exception in CopyOnClipboard()", ex.StackTrace); }
        }

        public void CopyOnClipboard()
        {
            try
            {
                if (!isEmpty)
                {
                    if (IsAudio)
                    {
                        Clipboard.SetAudio(Audio);
                    }
                    else if (IsText)
                    {
                        Clipboard.SetText(Text);
                    }
                    else if (IsFile)
                    {

                        //Impostare il l'effetto taglia per rimuovere dai file temporanei
                        MemoryStream dropEffect = new MemoryStream();
                        dropEffect.Write(CUT_EFFECT, 0, CUT_EFFECT.Length);
                        DataObject data = new DataObject();
                        data.SetFileDropList(FileDropList);
                        data.SetData("Preferred DropEffect", dropEffect);
                        Clipboard.Clear();
                        Clipboard.SetDataObject(data, true);

                    }
                    else
                    {

                        MemoryStream ms = new MemoryStream(Image);
                        ms.Position = 0;
                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = ms;
                        image.EndInit();
                        Clipboard.SetImage(image);
                    }
                }
            }
            catch (Exception ex) { Trace.TraceError("Exception in CopyOnClipboard()", ex.StackTrace); }
        }
    }
}
