using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HttpInvoker
{
    public class Pair
    {
        public string Name { get; set; }

        public string Value { get; set; }
    }

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        protected List<Pair> Pairs;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Pairs = new List<Pair>();
            grd.DataContext = Pairs;
        }

        private void DataGrid_InitializingNewItem(object sender, InitializingNewItemEventArgs e)
        {
            ((Pair)e.NewItem).Name = "";
            ((Pair)e.NewItem).Value = "";
        }

        private void grd_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                grd.BeginEdit();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string url = FixUrl(txtUrl.Text);
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Url not allow empty!");
                return;
            }

            rtbRequestHeader.Document.Blocks.Clear();
            rtbRequestBody.Document.Blocks.Clear();
            rtbResponseHeader.Document.Blocks.Clear();
            rtbResponseBody.Document.Blocks.Clear();
            pb.IsIndeterminate = true;

            string method = ((ComboBoxItem)cmbType.SelectedItem).Content.ToString();
            var items = grd.DataContext as List<Pair>;
            ThreadPool.QueueUserWorkItem((x) =>
            {
                try
                {
                    Invoke(url, method, items);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    window.Dispatcher.Invoke(new Action(() =>
                    {
                        pb.IsIndeterminate = false;
                    }));
                }
            });
        }

        public void Invoke(string url, string method, List<Pair> items)
        {
            StringBuilder builder = new StringBuilder();
            if (items != null)
            {
                foreach (var item in items)
                {
                    builder.AppendFormat("{0}={1}&", item.Name, item.Value);
                }
            }

            HttpRequest request = null;
            if ("post".Equals(method, StringComparison.OrdinalIgnoreCase))
            {
                request = new HttpRequest(url);
                request.WebRequest.Method = "POST";
                request.SetPostData(builder.ToString(), Encoding.UTF8);
            }
            else
            {
                request = new HttpRequest(url + "?" + builder.ToString());
                request.WebRequest.Method = "GET";
            }

            HttpDownload down = new HttpDownload(request);
            HttpResult result = down.GetResponse();

            window.Dispatcher.Invoke(new Action(() =>
            {
                rtbRequestHeader.AppendText(ParseHeader(request.WebRequest.Headers));
                rtbRequestBody.AppendText(builder.ToString());
                rtbResponseHeader.AppendText(ParseHeader(result.Headers));
                rtbResponseBody.AppendText(result.ResponseString);
                pb.IsIndeterminate = false;
            }));
        }

        public string ParseHeader(WebHeaderCollection headers)
        {
            StringBuilder builder = new StringBuilder();

            if (headers != null)
            {
                foreach (var item in headers.AllKeys)
                {
                    builder.AppendFormat("{0} : {1}{2}", item, headers.Get(item), Environment.NewLine);
                }
            }

            return builder.ToString();
        }

        public string FixUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            url = url.Trim();

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                return url;

            if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return url;

            url = "http://" + url;
            return url;
        }
    }
}
