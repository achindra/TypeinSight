using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TypeinSight;

namespace LoginWindow
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {
      private uint[] KeyDownTime = new uint[256];
      private static DateTime baseDate = new DateTime(1970, 1, 1);
      private static double epoc = (DateTime.UtcNow - baseDate).TotalMilliseconds;
      private static ulong BootTime = (ulong)(epoc - Environment.TickCount);

      private nGrams _1Grams = new nGrams(1);
      private nGrams _3Grams = new nGrams(3);
      private nGrams _5Grams = new nGrams(5);
      private TypedWord _typedWord = new TypedWord();

      private static Queue msgQueue = new Queue();
      private static Thread msgThread = new Thread(ThreadProc);
      private static ManualResetEventSlim dataQueued = new ManualResetEventSlim(false);
      private static ManualResetEventSlim procDead = new ManualResetEventSlim(false);

      public MainWindow()
      {
         InitializeComponent();
         txtLogin.Focus();
         msgThread.Start();
      }

      private void txtPassword_KeyDown(object sender, KeyEventArgs e)
      {
         int i = KeyInterop.VirtualKeyFromKey(e.Key);
         KeyDownTime[i] = (uint) e.Timestamp;
      }

      private void txtPassword_KeyUp(object sender, KeyEventArgs e)
      {
         uint i =  (uint) KeyInterop.VirtualKeyFromKey(e.Key);
         uint _keyStart = KeyDownTime[i];

         //skip Caps, Shifts, Ctrls, Window, Alts, RClicks
         char c = Convert.ToChar(i);
         if ((c >= 160 && c <= 165) || (c >= 91 && c <= 93) || c == 9 || c == 20)
            return;

         TypedData _1GData = _1Grams.GetTypedData(i, (uint)e.Timestamp - _keyStart);
         if (null != _1GData && null != _1GData.strData)
         {
            msgQueue.Enqueue(_1GData);
            dataQueued.Set();
         }

         TypedData _3GData = _3Grams.GetTypedData(i, (uint)e.Timestamp - _keyStart);
         if (null != _3GData && null != _3GData.strData)
         {
            msgQueue.Enqueue(_3GData);
            dataQueued.Set();
         }

         TypedData _5GData = _5Grams.GetTypedData(i, (uint)e.Timestamp - _keyStart);
         if (null != _5GData && null != _5GData.strData)
         {
            msgQueue.Enqueue(_5GData);
            dataQueued.Set();
         }

         TypedData _TyData = _typedWord.GetTypedData(i, (uint)e.Timestamp - _keyStart);
         if (null != _TyData && null != _TyData.strData)
         {
            msgQueue.Enqueue(_TyData);
            dataQueued.Set();
            //MLWebClient.InvokeRequestResponseService(_TyData.strData, (int)_TyData.strTime, (int)_TyData.grams).Wait();
         }
      }

      private void btnClear_Click(object sender, RoutedEventArgs e)
      {
         txtPassword.Password = "";
         txtLogin.Text = "";
         txtLogin.Focus();
      }

      private static void ThreadProc()
      {
         while (!procDead.IsSet)
         {
            dataQueued.Wait();
            while (msgQueue.Count != 0 && !procDead.IsSet)
            {
               TypedData data = (TypedData)msgQueue.Dequeue();

               MLWebClient.InvokeRequestResponseService(data.strData, data.strTime, data.grams);
            }
            if (msgQueue.Count == 0)
               dataQueued.Reset();
         }
      }
   }
}
