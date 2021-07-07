using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCS.Models
{
    public class Thumbnail
    {
        public TimeSpan TimeSpan { get; private set; }

        public Image Image { get; private set; }

        public Thumbnail(TimeSpan timeSpan, Image image)
        {
            this.TimeSpan = timeSpan;
            this.Image = image;
        }
    }
}
