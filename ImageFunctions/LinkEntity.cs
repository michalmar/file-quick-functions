using Microsoft.Azure.Cosmos.Table;
using System;
using System.Globalization;

namespace ImageFunctions
{
    public class LinkEntity : TableEntity
    {
        public string SASLink { get; set; }
        public string ShareFileName { get; set; }
        public string ShortLink { get; set; }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "\"{0}\" by {1} ({2})",
                this.ShareFileName,
                this.ShortLink,
                this.SASLink);
        }
    }
}
