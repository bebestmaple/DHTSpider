namespace Tancoder.Torrent
{
    public interface IMetaDataFilter
    {
        bool Ignore(InfoHash metadata);
    }
}