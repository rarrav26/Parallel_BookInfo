using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Program;

namespace Parallel_BookInfo
{
    internal class BookInfo
    {
        public BookInfo(int rowNumber, DataRetrievalType dataRetreivalType, string iSBN, string title,
            string subtitle, string authorNames, string numberOfPages, string publishDate)
        {
            RowNumber = rowNumber;
            DataRetreivalType = dataRetreivalType;
            ISBN = iSBN;
            Title = title;
            Subtitle = subtitle;
            AuthorNames = authorNames;
            NumberOfPages = numberOfPages;
            PublishDate = publishDate;
        }

        public int RowNumber { get; set; }
        public DataRetrievalType DataRetreivalType { get; set; }
        public string ISBN { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string AuthorNames { get; set; }
        public string NumberOfPages { get; set; }
        public string PublishDate { get; set; }

        public string ToCSVFormat()
        {
            return $"{RowNumber};{(this.DataRetreivalType == DataRetrievalType.Server ? nameof(DataRetrievalType.Server) : nameof(DataRetrievalType.Cache))};{ISBN};{Title};{(!string.IsNullOrEmpty(Subtitle) ? Subtitle : "N/A")};\"{AuthorNames}\";{(!string.IsNullOrEmpty(NumberOfPages) ? NumberOfPages : "N/A")};{PublishDate}";
        }
    }
}