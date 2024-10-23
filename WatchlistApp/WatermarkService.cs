using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WatchlistApp
{
    public class WatermarkService
    {
        public static readonly DependencyProperty WatermarkProperty = 
            DependencyProperty.RegisterAttached("Watermark", typeof(string), typeof(WatermarkService), new PropertyMetadata(string.Empty));

        public static string GetWatermark(DependencyObject obj)
        {
            return (string)obj.GetValue(WatermarkProperty);
        }

        public static void SetWatermark(DependencyObject obj, string value)
        {
            obj.SetValue(WatermarkProperty, value);
        }
    }
}
