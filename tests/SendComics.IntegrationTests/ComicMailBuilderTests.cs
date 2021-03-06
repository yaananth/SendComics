namespace SendComics.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FakeItEasy;
    using FluentAssertions;
    using SelfInitializingFakes;
    using global::SendComics.Services;
    using SendGrid.Helpers.Mail;
    using Xunit;

    public class ComicMailBuilderTests
    {
        private const string DilbertImageUrl = "https://assets.amuniversal.com/cfa39b00b39601365f19005056a9545d";
        private const string ChickweedLaneUrl = "https://assets.amuniversal.com/e2a3c500c015013663ff005056a9545d";
        private const string BlondieUrl = "https://safr.kingfeatures.com/idn/cnfeed/zone/js/content.php?file=aHR0cDovL3NhZnIua2luZ2ZlYXR1cmVzLmNvbS9CbG9uZGllLzIwMTgvMDQvQmxvbmRpZS4yMDE4MDQxMF85MDAuZ2lm";
        private const string RhymesWithOrangeUrl = "https://safr.kingfeatures.com/idn/cnfeed/zone/js/content.php?file=aHR0cDovL3NhZnIua2luZ2ZlYXR1cmVzLmNvbS9SaHltZXNXaXRoT3JhbmdlLzIwMTgvMDQvUmh5bWVzX3dpdGhfT3JhbmdlLjIwMTgwNDEwXzkwMC5naWY=";
        private const string CalvinAndHobbesSundayUrl = "https://assets.amuniversal.com/65839a905f980136408e005056a9545d";

        [Fact]
        public void OneSubscriberTwoComics_BuildsOneMailWithBothComics()
        {
            IList<Mail> mails = null;

            using (var fakeComicFetcher = SelfInitializingFake<IComicFetcher>.For(
                () => new WebComicFetcher(),
                new XmlFileRecordedCallRepository("../../../RecordedCalls/OneSubscriberTwoComics_BuildsOneMailWithBothComics.xml")))
            {
                var target = new ComicMailBuilder(
                    DateTime.Now,
                    new SimpleConfigurationParser("blair.conrad@gmail.com: dilbert, 9chickweedlane"),
                    fakeComicFetcher.Object,
                    A.Dummy<ILogger>());
            
                mails = target.CreateMailMessage().ToList();
             }

            mails.Should().HaveCount(1);

            mails[0].From.Address.Should().Be("comics@blairconrad.com");
            mails[0].Personalization[0].Tos.Should().HaveCount(1);
            mails[0].Personalization[0].Tos[0].Address.Should().Be("blair.conrad@gmail.com");
            mails[0].Contents[0].Value.Should().Contain(DilbertImageUrl);
            mails[0].Contents[0].Value.Should().Contain(ChickweedLaneUrl);
        }

        [Fact]
        public void TwoSubscribersOneComicEach_BuildsTwoMailsEachWithOneComic()
        {
            IList<Mail> mails = null;

            using (var fakeComicFetcher = SelfInitializingFake<IComicFetcher>.For(
                () => new WebComicFetcher(),
                new XmlFileRecordedCallRepository("../../../RecordedCalls/TwoSubscribersOneComicEach_BuildsTwoMailsEachWithOneComic.xml")))
            {
                var target = new ComicMailBuilder(
                    DateTime.Now,
                    new SimpleConfigurationParser("blair.conrad@gmail.com: 9chickweedlane; anyone@mail.org: dilbert"),
                    fakeComicFetcher.Object,
                    A.Dummy<ILogger>());

                mails = target.CreateMailMessage().ToList();
            }

            mails.Should().HaveCount(2);

            mails[0].From.Address.Should().Be("comics@blairconrad.com");
            mails[0].Personalization[0].Tos.Should().HaveCount(1);
            mails[0].Personalization[0].Tos[0].Address.Should().Be("blair.conrad@gmail.com");
            mails[0].Contents[0].Value.Should().Contain(ChickweedLaneUrl);

            mails[1].From.Address.Should().Be("comics@blairconrad.com");
            mails[1].Contents[0].Value.Should().Contain(DilbertImageUrl);
            mails[1].Personalization[0].Tos.Should().HaveCount(1);
            mails[1].Personalization[0].Tos[0].Address.Should().Be("anyone@mail.org");
        }

        [Fact]
        public void SubscribesToKingsFeatureComics_BuildsOneMailWithBothComics()
        {
            IList<Mail> mails = null;

            using (var fakeComicFetcher = SelfInitializingFake<IComicFetcher>.For(
                () => new WebComicFetcher(),
                new XmlFileRecordedCallRepository("../../../RecordedCalls/SubscribesToKingsFeatureComics_BuildsOneMailWithBothComics.xml")))
            {
                var target = new ComicMailBuilder(
                    DateTime.Now,
                    new SimpleConfigurationParser("blair.conrad@gmail.com: blondie, rhymeswithorange"),
                    fakeComicFetcher.Object,
                    A.Dummy<ILogger>());

                mails = target.CreateMailMessage().ToList();
            }

            mails.Should().HaveCount(1);

            mails[0].Contents[0].Value.Should()
                .Contain(BlondieUrl, "it should have Blondie").And
                .Contain(RhymesWithOrangeUrl, "it should have Rhymes with Orange");
        }

        [Theory]
        [InlineData("dilbert", "http://www.dilbert.com/")]
        [InlineData("blondie", "http://blondie.com/")]
        [InlineData("9chickweedlane", "http://www.gocomics.com/9chickweedlane/2018/06/27/")]
        public void SubscribesToOneComic_QueriesFetcherWithCorrectUrl(string comic, string expectedUrl)
        {
            var fakeComicFetcher = A.Fake<IComicFetcher>();

            var target = new ComicMailBuilder(
                new DateTime(2018, 6, 27),
                new SimpleConfigurationParser($"blair.conrad@gmail.com: {comic}"),
                fakeComicFetcher,
                A.Dummy<ILogger>());

            target.CreateMailMessage().ToList();

            A.CallTo(() => fakeComicFetcher.GetContent(expectedUrl)).MustHaveHappened();
        }

        [Theory]
        [InlineData(DayOfWeek.Saturday)]
        [InlineData(DayOfWeek.Sunday)]
        public void DinosaurComicOnAWeekend_MailIndicatesComicNotPublishedToday(DayOfWeek dayOfWeek)
        {
            IList<Mail> mails = null;

            using (var fakeComicFetcher = SelfInitializingFake<IComicFetcher>.For(
                () => new WebComicFetcher(),
                new XmlFileRecordedCallRepository("../../../RecordedCalls/DinosaurComicsOn" + dayOfWeek + ".xml")))
            {
                var dateToCheck = MostRecent(dayOfWeek);
                var target = new ComicMailBuilder(
                    dateToCheck,
                    new SimpleConfigurationParser("blair.conrad@gmail.com: dinosaur-comics"),
                    fakeComicFetcher.Object,
                    A.Dummy<ILogger>());

                mails = target.CreateMailMessage().ToList();
            }

            mails.Should().HaveCount(1);

            mails[0].Contents[0].Value.Should()
                .NotContain("Couldn't find comic for dinosaur-comics.", "it should not have looked for the comic").And
                .Contain("Comic dinosaur-comics wasn't published today.", "it should tell the reader why there's no comic");
        }

        [Theory]
        [InlineData(DayOfWeek.Monday)]
        [InlineData(DayOfWeek.Tuesday)]
        [InlineData(DayOfWeek.Wednesday)]
        [InlineData(DayOfWeek.Thursday)]
        [InlineData(DayOfWeek.Friday)]
        public void DinosaurComicOnAWeekday_MailIncludesComic(DayOfWeek dayOfWeek)
        {
            IList<Mail> mails = null;

            using (var fakeComicFetcher = SelfInitializingFake<IComicFetcher>.For(
                () => new WebComicFetcher(),
                new XmlFileRecordedCallRepository("../../../RecordedCalls/DinosaurComicsOn" + dayOfWeek + ".xml")))
            {
                var dateToCheck = MostRecent(dayOfWeek);
                var target = new ComicMailBuilder(
                    dateToCheck,
                    new SimpleConfigurationParser("blair.conrad@gmail.com: dinosaur-comics"),
                    fakeComicFetcher.Object,
                    A.Dummy<ILogger>());

                mails = target.CreateMailMessage().ToList();
            }

            mails.Should().HaveCount(1);

            mails[0].Contents[0].Value.Should()
                .NotContain("Couldn't find comic for dinosaur-comics.", "it should not have looked for the comic").And
                .NotContain("Comic dinosaur-comics wasn't published today.", "it should have found the comic");
        }

        [Theory]
        [InlineData(DayOfWeek.Monday)]
        [InlineData(DayOfWeek.Tuesday)]
        [InlineData(DayOfWeek.Wednesday)]
        [InlineData(DayOfWeek.Thursday)]
        [InlineData(DayOfWeek.Friday)]
        [InlineData(DayOfWeek.Saturday)]
        public void FoxtrotOnAnythingButSunday_MailIndicatesComicNotPublishedToday(DayOfWeek dayOfWeek)
        {
            IList<Mail> mails = null;

            using (var fakeComicFetcher = SelfInitializingFake<IComicFetcher>.For(
                () => new WebComicFetcher(),
                new XmlFileRecordedCallRepository("../../../RecordedCalls/FoxTrotOn" + dayOfWeek + ".xml")))
            {
                var dateToCheck = MostRecent(dayOfWeek);
                var target = new ComicMailBuilder(
                    dateToCheck,
                    new SimpleConfigurationParser("blair.conrad@gmail.com: foxtrot"),
                    fakeComicFetcher.Object,
                    A.Dummy<ILogger>());

                mails = target.CreateMailMessage().ToList();
            }

            mails.Should().HaveCount(1);

            mails[0].Contents[0].Value.Should()
                .NotContain("Couldn't find comic for foxtrot.", "it should not have looked for the comic").And
                .Contain("Comic foxtrot wasn't published today.", "it should tell the reader why there's no comic");
        }

        [Fact]
        public void FoxtrotOnSunday_MailIncludesComic()
        {
            IList<Mail> mails = null;

            using (var fakeComicFetcher = SelfInitializingFake<IComicFetcher>.For(
                () => new WebComicFetcher(),
                new XmlFileRecordedCallRepository("../../../RecordedCalls/FoxTrotOnSunday.xml")))
            {
                var dateToCheck = MostRecent(DayOfWeek.Sunday);
                var target = new ComicMailBuilder(
                    dateToCheck,
                    new SimpleConfigurationParser("blair.conrad@gmail.com: foxtrot"),
                    fakeComicFetcher.Object,
                    A.Dummy<ILogger>());

                mails = target.CreateMailMessage().ToList();
            }

            mails.Should().HaveCount(1);

            mails[0].Contents[0].Value.Should()
                .NotContain("Couldn't find comic for foxtrot.", "it should not have looked for the comic").And
                .NotContain("Comic foxtrot wasn't published today.", "it should have found the comic");
        }

        [Fact]
        public void CalvinAndHobbesOnSunday_MailIncludesComic()
        {
            IList<Mail> mails = null;

            using (var fakeComicFetcher = SelfInitializingFake<IComicFetcher>.For(
                () => new WebComicFetcher(),
                new XmlFileRecordedCallRepository("../../../RecordedCalls/CalvinAndHobbesOnSunday.xml")))
            {
                var dateToCheck = MostRecent(DayOfWeek.Sunday);
                var target = new ComicMailBuilder(
                    dateToCheck,
                    new SimpleConfigurationParser("blair.conrad@gmail.com: calvinandhobbes"),
                    fakeComicFetcher.Object,
                    A.Dummy<ILogger>());

                mails = target.CreateMailMessage().ToList();
            }

            mails.Should().HaveCount(1);

            mails[0].Contents[0].Value.Should()
                .NotContain("Couldn't find comic for calvinandhobbes.", "it should not have looked for the comic").And
                .Contain(CalvinAndHobbesSundayUrl, "it should have found the comic");
        }

        private static DateTime MostRecent(DayOfWeek dayOfWeek)
        {
            var now = DateTime.Now;
            var offset = (int)dayOfWeek - (int)now.DayOfWeek;
            if (offset > 0)
            {
                offset -= 7;
            }

            return now.AddDays(offset);
        }
    }
}
