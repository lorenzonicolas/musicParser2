using Moq;
using musicParser.MetalArchives;
using musicParser.Utils.Loggers;
using MusicParser.Utils.HttpClient;
using Newtonsoft.Json;

namespace MusicParser.Tests.MetalArchivesAPI
{
    [TestFixture]
    [Parallelizable]
    public class MetalArchivesServiceTests
    {
        readonly Mock<IExecutionLogger> logger = new();
        readonly Mock<IMetalArchivesAPI> api = new();
        readonly Mock<IHttpClient> http = new();
        private const string BAND_NAME = "Emperor";
        private const string ALBUM_NAME = "Anthems to the Welkin at Dusk";

        [TearDown]
        public void TearDown()
        {
            api.Invocations.Clear();
            http.Invocations.Clear();
        }

        [Test]
        public async Task GetAlbumYear_NullValue()
        {
            var searchResponse = new searchBandResponse()
            {
                success = true,
                data = new SearchBand()
                {
                    totalResults = 0
                }
            };

            api.Setup(o => o.Search("name", BAND_NAME)).ReturnsAsync(JsonConvert.SerializeObject(searchResponse));

            var service = new MetalArchivesService(logger.Object, api.Object, http.Object);
            
            // Act
            var result = await service.GetAlbumYearAsync(BAND_NAME, ALBUM_NAME);

            // Assert
            Assert.That(result, Is.Null);
            api.Verify(c => c.Search("name", BAND_NAME), Times.Once());
            api.Verify(c => c.GetBandDiscography(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public async Task GetAlbumYear_ExceptionReturnsNull()
        {
            api.Setup(o => o.Search("name", BAND_NAME)).ThrowsAsync(new Exception());
            var service = new MetalArchivesService(logger.Object, api.Object, http.Object);

            // Act
            var result = await service.GetAlbumYearAsync(BAND_NAME, ALBUM_NAME);

            // Assert
            Assert.That(result, Is.Null);
            api.Verify(c => c.Search("name", BAND_NAME), Times.Once());
            api.Verify(c => c.GetBandDiscography(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public async Task GetAlbumYear_NullWithFoundArtists()
        {
            var searchResponse = new searchBandResponse()
            {
                success = true,
                data = new SearchBand()
                {
                    totalResults = 2,
                    bands = new BandResult[]
                    {
                        new BandResult()
                        {
                            Name = BAND_NAME,
                            Id = "id",
                            Country = "Norway",
                            Genre = "Black Metal"
                        },
                        new BandResult()
                        {
                            Name = "OTHER",
                            Id = "id2",
                            Country = "Norway",
                            Genre = "Black Metal"
                        }
                    }
                }
            };

            var searchDiscographyResponse = new searchDiscographyResponse()
            {
                success = true,
                data = new SearchDiscography()
                {
                    discography = new AlbumResult[]
                    {
                        new AlbumResult() { name = "some album name" }
                    }
                }
            };

            api.Setup(o => o.Search("name", "UNKOWN_BAND")).ReturnsAsync(JsonConvert.SerializeObject(searchResponse));
            api.Setup(o => o.GetBandDiscography("id")).ReturnsAsync(JsonConvert.SerializeObject(searchDiscographyResponse));
            api.Setup(o => o.GetBandDiscography("id2")).ReturnsAsync(JsonConvert.SerializeObject(searchDiscographyResponse));

            var service = new MetalArchivesService(logger.Object, api.Object, http.Object);

            // Act
            var result = await service.GetAlbumYearAsync("UNKOWN_BAND", "UNKOWN_ALBUM");

            // Assert
            Assert.That(result, Is.Null);
            api.Verify(c => c.Search("name", "UNKOWN_BAND"), Times.Once());
            api.Verify(c => c.GetBandDiscography("id"), Times.Once());
            api.Verify(c => c.GetBandDiscography("id2"), Times.Once());
        }

        [Test]
        public async Task GetAlbumYear_Found()
        {
            var searchResponse = new searchBandResponse()
            {
                success = true,
                data = new SearchBand()
                {
                    totalResults = 2,
                    bands = new BandResult[]
                    {
                        new BandResult()
                        {
                            Name = BAND_NAME,
                            Id = "id",
                            Country = "Norway",
                            Genre = "Black Metal"
                        },
                        new BandResult()
                        {
                            Name = "OTHER",
                            Id = "id2",
                            Country = "Norway",
                            Genre = "Black Metal"
                        }
                    }
                }
            };

            var searchDiscographyResponse = new searchDiscographyResponse()
            {
                success = true,
                data = new SearchDiscography()
                {
                    discography = new AlbumResult[]
                    {
                        new AlbumResult() { name = ALBUM_NAME, year = "1995" }
                    }
                }
            };

            var searchDiscographyResponse2 = new searchDiscographyResponse() { success = false };

            api.Setup(o => o.Search("name", BAND_NAME)).ReturnsAsync(JsonConvert.SerializeObject(searchResponse));
            api.Setup(o => o.GetBandDiscography("id2")).ReturnsAsync(JsonConvert.SerializeObject(searchDiscographyResponse));
            api.Setup(o => o.GetBandDiscography("id")).ReturnsAsync(JsonConvert.SerializeObject(searchDiscographyResponse2));

            var service = new MetalArchivesService(logger.Object, api.Object, http.Object);

            // Act
            var result = await service.GetAlbumYearAsync(BAND_NAME, ALBUM_NAME);

            // Assert
            Assert.That(result, Is.EqualTo("1995"));
            api.Verify(c => c.Search("name", BAND_NAME), Times.Once());
            api.Verify(c => c.GetBandDiscography("id"), Times.Once());
            api.Verify(c => c.GetBandDiscography("id2"), Times.Once());
        }

        [Test]
        public async Task GetBandCountry_Unknown()
        {
            var searchResponse = new searchBandResponse()
            {
                success = true,
                data = new SearchBand()
                {
                    totalResults = 0
                }
            };

            api.Setup(o => o.Search("name", BAND_NAME)).ReturnsAsync(JsonConvert.SerializeObject(searchResponse));

            var service = new MetalArchivesService(logger.Object, api.Object, http.Object);

            // Act
            var result = await service.GetBandCountryAsync(BAND_NAME);

            // Assert
            Assert.That(result, Is.EqualTo("Unknown"));
            api.Verify(c => c.Search("name", BAND_NAME), Times.Once());
            api.Verify(c => c.GetBandDiscography(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public async Task GetBandCountry_ExceptionReturnsUnknown()
        {
            api.Setup(o => o.Search("name", BAND_NAME)).ThrowsAsync(new Exception());
            var service = new MetalArchivesService(logger.Object, api.Object, http.Object);

            // Act
            var result = await service.GetBandCountryAsync(BAND_NAME);

            // Assert
            Assert.That(result, Is.EqualTo("Unknown"));
            api.Verify(c => c.Search("name", BAND_NAME), Times.Once());
            api.Verify(c => c.GetBandDiscography(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public async Task GetBandCountry_PerfectMatch()
        {
            var searchResponse = new searchBandResponse()
            {
                success = true,
                data = new SearchBand()
                {
                    totalResults = 1,
                    bands = new BandResult[] { new BandResult() { Country = "Norway" } }
                }
            };

            api.Setup(o => o.Search("name", BAND_NAME)).ReturnsAsync(JsonConvert.SerializeObject(searchResponse));

            var service = new MetalArchivesService(logger.Object, api.Object, http.Object);

            // Act
            var result = await service.GetBandCountryAsync(BAND_NAME);

            // Assert
            Assert.That(result, Is.EqualTo("Norway"));
            api.Verify(c => c.Search("name", BAND_NAME), Times.Once());
            api.Verify(c => c.GetBandDiscography(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public async Task GetBandCountry_ManyBands_ButNoAlbumGiven()
        {
            var searchResponse = new searchBandResponse()
            {
                success = true,
                data = new SearchBand()
                {
                    totalResults = 2,
                    bands = new BandResult[] { new BandResult() { }, new BandResult() { } }
                }
            };

            api.Setup(o => o.Search("name", BAND_NAME)).ReturnsAsync(JsonConvert.SerializeObject(searchResponse));

            var service = new MetalArchivesService(logger.Object, api.Object, http.Object);

            // Act
            var result = await service.GetBandCountryAsync(BAND_NAME);

            // Assert
            Assert.That(result, Is.EqualTo("Unknown"));
            api.Verify(c => c.Search("name", BAND_NAME), Times.Once());
            api.Verify(c => c.GetBandDiscography(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public async Task GetBandCountry_ManyBands_ButNoMatch()
        {
            var searchResponse = new searchBandResponse()
            {
                success = true,
                data = new SearchBand()
                {
                    totalResults = 2,
                    bands = new BandResult[]
                    {
                        new BandResult() { Id = "id1"},
                        new BandResult() { Id = "id2"}
                    }
                }
            };

            var searchDiscographyResponse = new searchDiscographyResponse()
            {
                success = true,
                data = new SearchDiscography()
                {
                    discography = new AlbumResult[]
                    {
                        new AlbumResult() { name = "some album name" }
                    }
                }
            };

            api.Setup(o => o.GetBandDiscography("id1")).ReturnsAsync(JsonConvert.SerializeObject(searchDiscographyResponse));
            api.Setup(o => o.GetBandDiscography("id2")).ReturnsAsync(JsonConvert.SerializeObject(searchDiscographyResponse));
            api.Setup(o => o.Search("name", BAND_NAME)).ReturnsAsync(JsonConvert.SerializeObject(searchResponse));

            var service = new MetalArchivesService(logger.Object, api.Object, http.Object);

            // Act
            var result = await service.GetBandCountryAsync(BAND_NAME, ALBUM_NAME);

            // Assert
            Assert.That(result, Is.EqualTo("Unknown"));
            api.Verify(c => c.Search("name", BAND_NAME), Times.Once());
            api.Verify(c => c.GetBandDiscography("id1"), Times.Once());
            api.Verify(c => c.GetBandDiscography("id2"), Times.Once());
        }

        [Test]
        public async Task GetBandCountry_ManyBands_Match()
        {
            var searchResponse = new searchBandResponse()
            {
                success = true,
                data = new SearchBand()
                {
                    totalResults = 2,
                    bands = new BandResult[]
                    {
                        new BandResult() { Id = "id1"},
                        new BandResult() { Id = "id2", Country = "Germany"}
                    }
                }
            };

            var searchDiscographyResponse = new searchDiscographyResponse()
            {
                success = true,
                data = new SearchDiscography()
                {
                    discography = new AlbumResult[]
                    {
                        new AlbumResult() { name = "some album name" }
                    }
                }
            };

            var searchDiscographyResponse2 = new searchDiscographyResponse()
            {
                success = true,
                data = new SearchDiscography()
                {
                    discography = new AlbumResult[]
                    {
                        new AlbumResult() { name = ALBUM_NAME }
                    }
                }
            };

            api.Setup(o => o.GetBandDiscography("id1")).ReturnsAsync(JsonConvert.SerializeObject(searchDiscographyResponse));
            api.Setup(o => o.GetBandDiscography("id2")).ReturnsAsync(JsonConvert.SerializeObject(searchDiscographyResponse2));
            api.Setup(o => o.Search("name", BAND_NAME)).ReturnsAsync(JsonConvert.SerializeObject(searchResponse));

            var service = new MetalArchivesService(logger.Object, api.Object, http.Object);

            // Act
            var result = await service.GetBandCountryAsync(BAND_NAME, ALBUM_NAME);

            // Assert
            Assert.That(result, Is.EqualTo("Germany"));
            api.Verify(c => c.Search("name", BAND_NAME), Times.Once());
            api.Verify(c => c.GetBandDiscography("id1"), Times.Once());
            api.Verify(c => c.GetBandDiscography("id2"), Times.Once());
        }

        #region GetBandGenre

        [Test]
        public async Task GetBandGenre_Unknown()
        {
            var searchResponse = new searchBandResponse()
            {
                success = true,
                data = new SearchBand()
                {
                    totalResults = 0
                }
            };

            api.Setup(o => o.Search("name", BAND_NAME)).ReturnsAsync(JsonConvert.SerializeObject(searchResponse));

            var service = new MetalArchivesService(logger.Object, api.Object, http.Object);

            // Act
            var result = await service.GetBandGenreAsync(BAND_NAME);

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
            api.Verify(c => c.Search("name", BAND_NAME), Times.Once());
            api.Verify(c => c.GetBandDiscography(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public async Task GetBandGenre_ExceptionReturnsUnknown()
        {
            api.Setup(o => o.Search("name", BAND_NAME)).ThrowsAsync(new Exception());
            var service = new MetalArchivesService(logger.Object, api.Object, http.Object);

            // Act
            var result = await service.GetBandGenreAsync(BAND_NAME);

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
            api.Verify(c => c.Search("name", BAND_NAME), Times.Once());
            api.Verify(c => c.GetBandDiscography(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public async Task GetBandGenre_PerfectMatch()
        {
            var searchResponse = new searchBandResponse()
            {
                success = true,
                data = new SearchBand()
                {
                    totalResults = 1,
                    bands = new BandResult[] { new BandResult() { Country = "Norway", Genre = "Black Metal" } }
                }
            };

            api.Setup(o => o.Search("name", BAND_NAME)).ReturnsAsync(JsonConvert.SerializeObject(searchResponse));

            var service = new MetalArchivesService(logger.Object, api.Object, http.Object);

            // Act
            var result = await service.GetBandGenreAsync(BAND_NAME);

            // Assert
            Assert.That(result, Is.EqualTo("Black Metal"));
            api.Verify(c => c.Search("name", BAND_NAME), Times.Once());
            api.Verify(c => c.GetBandDiscography(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public async Task GetBandGenre_ManyBands_ButNoAlbumGiven()
        {
            var searchResponse = new searchBandResponse()
            {
                success = true,
                data = new SearchBand()
                {
                    totalResults = 2,
                    bands = new BandResult[] { new BandResult() { }, new BandResult() { } }
                }
            };

            api.Setup(o => o.Search("name", BAND_NAME)).ReturnsAsync(JsonConvert.SerializeObject(searchResponse));

            var service = new MetalArchivesService(logger.Object, api.Object, http.Object);

            // Act
            var result = await service.GetBandGenreAsync(BAND_NAME);

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
            api.Verify(c => c.Search("name", BAND_NAME), Times.Once());
            api.Verify(c => c.GetBandDiscography(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public async Task GetBandGenre_ManyBands_ButNoMatch()
        {
            var searchResponse = new searchBandResponse()
            {
                success = true,
                data = new SearchBand()
                {
                    totalResults = 2,
                    bands = new BandResult[]
                    {
                        new BandResult() { Id = "id1"},
                        new BandResult() { Id = "id2"}
                    }
                }
            };

            var searchDiscographyResponse = new searchDiscographyResponse()
            {
                success = true,
                data = new SearchDiscography()
                {
                    discography = new AlbumResult[]
                    {
                        new AlbumResult() { name = "some album name" }
                    }
                }
            };

            api.Setup(o => o.GetBandDiscography("id1")).ReturnsAsync(JsonConvert.SerializeObject(searchDiscographyResponse));
            api.Setup(o => o.GetBandDiscography("id2")).ReturnsAsync(JsonConvert.SerializeObject(searchDiscographyResponse));
            api.Setup(o => o.Search("name", BAND_NAME)).ReturnsAsync(JsonConvert.SerializeObject(searchResponse));

            var service = new MetalArchivesService(logger.Object, api.Object, http.Object);

            // Act
            var result = await service.GetBandGenreAsync(BAND_NAME, ALBUM_NAME);

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
            api.Verify(c => c.Search("name", BAND_NAME), Times.Once());
            api.Verify(c => c.GetBandDiscography("id1"), Times.Once());
            api.Verify(c => c.GetBandDiscography("id2"), Times.Once());
        }

        [Test]
        public async Task GetBandGenre_ManyBands_Match()
        {
            var searchResponse = new searchBandResponse()
            {
                success = true,
                data = new SearchBand()
                {
                    totalResults = 2,
                    bands = new BandResult[]
                    {
                        new BandResult() { Id = "id1"},
                        new BandResult() { Id = "id2", Country = "Germany", Genre = "Black Metal"}
                    }
                }
            };

            var searchDiscographyResponse = new searchDiscographyResponse()
            {
                success = true,
                data = new SearchDiscography()
                {
                    discography = new AlbumResult[]
                    {
                        new AlbumResult() { name = "some album name" }
                    }
                }
            };

            var searchDiscographyResponse2 = new searchDiscographyResponse()
            {
                success = true,
                data = new SearchDiscography()
                {
                    discography = new AlbumResult[]
                    {
                        new AlbumResult() { name = ALBUM_NAME }
                    }
                }
            };

            api.Setup(o => o.GetBandDiscography("id1")).ReturnsAsync(JsonConvert.SerializeObject(searchDiscographyResponse));
            api.Setup(o => o.GetBandDiscography("id2")).ReturnsAsync(JsonConvert.SerializeObject(searchDiscographyResponse2));
            api.Setup(o => o.Search("name", BAND_NAME)).ReturnsAsync(JsonConvert.SerializeObject(searchResponse));

            var service = new MetalArchivesService(logger.Object, api.Object, http.Object);

            // Act
            var result = await service.GetBandGenreAsync(BAND_NAME, ALBUM_NAME);

            // Assert
            Assert.That(result, Is.EqualTo("Black Metal"));
            api.Verify(c => c.Search("name", BAND_NAME), Times.Once());
            api.Verify(c => c.GetBandDiscography("id1"), Times.Once());
            api.Verify(c => c.GetBandDiscography("id2"), Times.Once());
        }

        #endregion
    }
}
