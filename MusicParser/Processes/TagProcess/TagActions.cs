using System.IO.Abstractions;
using DTO;
using TagLib;

namespace musicParser.TagProcess
{
    public partial class TagsProcess
    {
        private void ProcessFileActions(TagAction trackNumberAction, TagAction trackTitleAction, Tag tags, SongInfo info, IFileInfo songFile)
        {
            var updateFileTrack = trackNumberAction == TagAction.UpdateFile_TrackNumber;
            var updateFileTitle = trackTitleAction == TagAction.UpdateFile_TrackTitle;

            if (updateFileTrack || updateFileTitle)
            {
                var trackNumber = updateFileTrack ? tags.Track.ToString() : info.TrackNumber;
                var title = updateFileTitle ? tags.Title : info.Title;

                RenameSong(trackNumber, title, info.Extension, songFile);
            }
        }

        private void RenameSong(string trackNumber, string title, string extension, IFileInfo file)
        {
            if (file == null || file.DirectoryName == null)
            {
                _logger.LogError("Invalid file, couldn't rename song");
                Log("Invalid file, couldn't rename song");
                return;
            }

            var correctFileFormat = string.Format("{0} - {1}.{2}", trackNumber, title, extension);
            var destinationPath = Path.Combine(file.DirectoryName, correctFileFormat);

            Log($"Renaming \"{file.Name}\" to \"{correctFileFormat}\"");

            file.MoveTo(destinationPath);

            _logger.Log($"Renamed song file: {correctFileFormat}");
        }

        private bool ProcessTagAction(TagAction action, Tag tags, SongInfo info, string? band = null, string? metaGenre = null)
        {
            var tagSaveIsNeeded = false;

            switch (action)
            {
                case TagAction.UpdateTag_TrackNumber:
                    tagSaveIsNeeded = true;
                    tags.Track = Convert.ToUInt32(info.TrackNumber);
                    ConsoleLogger.Log("\tReplaced track number song tag");
                    break;

                case TagAction.UpdateTag_TrackTitle:
                    tagSaveIsNeeded = true;
                    tags.Title = info.Title;
                    ConsoleLogger.Log("\tReplaced title song tag");
                    break;

                case TagAction.UpdateTagWithMeta_Genre:
                    if(!string.IsNullOrEmpty(band) && !string.IsNullOrEmpty(metaGenre))
                    {
                        tagSaveIsNeeded = true;
                        tags.Genres = new string[] { metaGenre };
                        ConsoleLogger.Log("\tReplaced genre song tag with metadata: " + metaGenre);
                    }
                    break;

                case TagAction.RemoveComments:
                    tagSaveIsNeeded = true;
                    tags.Comment = string.Empty;
                    ConsoleLogger.Log("\tRemoved comments on this song");
                    break;

                default:
                    break;
            }

            return tagSaveIsNeeded;
        }

        private static TagAction GetTrackNumberAction(uint tagTrack, int fileTrack)
        {
            var action = TagAction.NoChanges;
            var tagTrackEmpty = string.IsNullOrEmpty(tagTrack.ToString()) || tagTrack == 0;
            var fileTrackEmpty = string.IsNullOrEmpty(fileTrack.ToString()) || fileTrack == 0;

            // Empty tag or file auto-solve
            if (tagTrackEmpty && !fileTrackEmpty)
                return TagAction.UpdateTag_TrackNumber;

            if (fileTrackEmpty && !tagTrackEmpty)
                return TagAction.UpdateFile_TrackNumber;

            // Both have value but are different
            if (tagTrack != fileTrack)
            {
                return TagAction.AskForChanges;

                //This could be a future improvement, separate the idea of user input and automatic functionality.
                //action = TakeTrackActionFromUser(tagTrack, fileTrack, action);
            }

            return action;
        }

        private TagAction GetTrackTitleAction(string tagTitle, string fileTitle)
        {
            var action = TagAction.NoChanges;

            //  Empty tag or file auto-solve
            if (string.IsNullOrEmpty(tagTitle) && !string.IsNullOrEmpty(fileTitle))
                return TagAction.UpdateTag_TrackTitle;

            if (string.IsNullOrEmpty(fileTitle) && !string.IsNullOrEmpty(tagTitle))
                return TagAction.UpdateFile_TrackTitle;

            if (TagsUtils.NamesAreDifferent(tagTitle, fileTitle))
            {
                action = TagAction.AskForChanges;

                //This could be a future improvement, separate the idea of user input and automatic functionality.
                //action = TakeTitleActionFromUser(tagTitle, fileTitle, action);
            }

            return action;
        }

        private TagAction GetTrackCommentsAction(string comments)
        {
            if (!string.IsNullOrEmpty(comments))
            {
                return TagAction.RemoveComments;
            }

            return TagAction.NoChanges;
        }

        private TagAction GetTrackGenreAction(string[] genres, string bandGenreMetadata)
        {
            if (TagsUtils.IsInvalidGenre(genres) || genres[0] != bandGenreMetadata)
            {
                return TagAction.UpdateTagWithMeta_Genre;
            }

            return TagAction.NoChanges;
        }

        enum TagAction
        {
            UpdateTag_TrackNumber, UpdateTag_TrackTitle, UpdateTag_AlbumTitle, UpdateTag_Genre, UpdateTagWithMeta_Genre,
            NoChanges,
            UpdateFile_TrackNumber, UpdateFile_TrackTitle, RemoveComments,
            AskForChanges
        }

        enum AlbumAction
        {
            UpdateTag_NewMetadataGenre, UpdateTag_Genre, UpdateTag_AlbumTitle, UpdateFolder_Name, NoChanges
        }
    }
}