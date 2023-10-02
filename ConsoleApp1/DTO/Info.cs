namespace musicParser.DTO
{
    public class AlbumInfoBackupDto
    {
        public string Band { get; set; }

        public string AlbumName { get; set; }

        public string Genre { get; set; }

        public string Year { get; set; }

        public string Type { get; set; }

        public string Country { get; set; }

        public DateTime DateBackup { get; set; }
    }

    public class MetadataDto
    {
        public string Band { get; set; }
        public string Genre { get; set; }
        public string Country { get; set; }
    }

    public class AlbumInfoOnDisk
    {
        public string Band { get; set; }
        public string AlbumName { get; set; }
        public DateTime LastTimeWrite { get; set; }
        public string FolderPath { get; set; }
        public string Year { get; set; }
        public string Name { get; set; }
    }
}
