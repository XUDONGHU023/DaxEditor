﻿namespace DaxEditor.Test
{
    using System.IO;
    using System.Collections.Generic;
    using System.ComponentModel.Composition.Hosting;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using Microsoft.VisualStudio.Text;

    [TestFixture]
    public class OutliningTaggerTests
    {
        [Test]
        public void OutliningTagger_EmptyBuffer()
        {
            var iTextBufferFactoryService = GetTextBufferFactoryService();
            var emptyTextBuffer = iTextBufferFactoryService.CreateTextBuffer();
            Assert.IsNotNull(emptyTextBuffer);
            var outliningTagger = new OutliningTagger(emptyTextBuffer);
            var spans = new NormalizedSnapshotSpanCollection(emptyTextBuffer.CurrentSnapshot, GetTextSnapshotSpan(emptyTextBuffer.CurrentSnapshot));
            var tags = outliningTagger.GetTags(spans);
            Assert.AreEqual(0, tags.Count());
        }

        [Test]
        public void OutliningTagger_OnlyComment()
        {
            var iTextBufferFactoryService = GetTextBufferFactoryService();
            var commentTextBuffer = iTextBufferFactoryService.CreateTextBuffer(@"-- this is comment only", iTextBufferFactoryService.TextContentType);
            Assert.IsNotNull(commentTextBuffer);
            var outliningTagger = new OutliningTagger(commentTextBuffer);
            var spans = new NormalizedSnapshotSpanCollection(commentTextBuffer.CurrentSnapshot, GetTextSnapshotSpan(commentTextBuffer.CurrentSnapshot));
            var tags = outliningTagger.GetTags(spans);
            Assert.AreEqual(0, tags.Count());
        }

        [Test]
        public void OutliningTagger_RandomText()
        {
            var iTextBufferFactoryService = GetTextBufferFactoryService();
            var textBuffer = iTextBufferFactoryService.CreateTextBuffer(@"asd", iTextBufferFactoryService.TextContentType);
            Assert.IsNotNull(textBuffer);
            var outliningTagger = new OutliningTagger(textBuffer);
            var spans = new NormalizedSnapshotSpanCollection(textBuffer.CurrentSnapshot, GetTextSnapshotSpan(textBuffer.CurrentSnapshot));
            var tags = outliningTagger.GetTags(spans);
            Assert.AreEqual(0, tags.Count());
        }

        [Test]
        public void OutliningTagger_OneLineQuery()
        {
            var iTextBufferFactoryService = GetTextBufferFactoryService();
            var OneLineQuery = iTextBufferFactoryService.CreateTextBuffer(@"EVALUATE T", iTextBufferFactoryService.TextContentType);
            Assert.IsNotNull(OneLineQuery);
            var outliningTagger = new OutliningTagger(OneLineQuery);
            var spans = new NormalizedSnapshotSpanCollection(OneLineQuery.CurrentSnapshot, GetTextSnapshotSpan(OneLineQuery.CurrentSnapshot));
            var tags = outliningTagger.GetTags(spans);
            Assert.AreEqual(1, tags.Count());
        }

        [Test]
        public void OutliningTagger_TwoMeasures()
        {
            var iTextBufferFactoryService = GetTextBufferFactoryService();
            var OneLineQuery = iTextBufferFactoryService.CreateTextBuffer(@"CREATE MEASURE T[M1] = 1
CREATE MEASURE 'T T'[M2] = 2", iTextBufferFactoryService.TextContentType);
            Assert.IsNotNull(OneLineQuery);
            var outliningTagger = new OutliningTagger(OneLineQuery);
            var spans = new NormalizedSnapshotSpanCollection(OneLineQuery.CurrentSnapshot, GetTextSnapshotSpan(OneLineQuery.CurrentSnapshot));
            var tags = outliningTagger.GetTags(spans);
            Assert.AreEqual(2, tags.Count());
            var tag1 = tags.First();
            Assert.AreEqual(0, tag1.Span.Start.Position);
            Assert.AreEqual(24, tag1.Span.End.Position);
            Assert.AreEqual("T[M1]", tag1.Tag.CollapsedForm as string);
            Assert.AreEqual("CREATE MEASURE T[M1] = 1", tag1.Tag.CollapsedHintForm as string);
            var tag2 = tags.Last();
            Assert.AreEqual(26, tag2.Span.Start.Position);
            Assert.AreEqual(54, tag2.Span.End.Position);
            Assert.AreEqual("'T T'[M2]", tag2.Tag.CollapsedForm as string);
            Assert.AreEqual("CREATE MEASURE 'T T'[M2] = 2", tag2.Tag.CollapsedHintForm as string);
        }

        [Test]
        public void OutliningTagger_ThreeMeasures()
        {
            var iTextBufferFactoryService = GetTextBufferFactoryService();
            var text = iTextBufferFactoryService.CreateTextBuffer(@"CREATE MEASURE T[M1] = 1;

CREATE MEASURE T[m3] = 
 (3 + 9) / 12

;

CREATE MEASURE T[M2] = 2
;

", iTextBufferFactoryService.TextContentType);
            Assert.IsNotNull(text);
            var outliningTagger = new OutliningTagger(text);
            var spans = new NormalizedSnapshotSpanCollection(text.CurrentSnapshot, GetTextSnapshotSpan(text.CurrentSnapshot));
            var tags = outliningTagger.GetTags(spans);
            Assert.AreEqual(3, tags.Count());

            var tag1 = tags.First();
            Assert.AreEqual(0, tag1.Span.Start.Position);
            Assert.AreEqual(25, tag1.Span.End.Position);
            var tag2 = tags.Skip(1).First();
            Assert.AreEqual(29, tag2.Span.Start.Position);
            Assert.AreEqual(72, tag2.Span.End.Position);
            var tag3 = tags.Skip(2).First();
            Assert.AreEqual(76, tag3.Span.Start.Position);
            Assert.AreEqual(103, tag3.Span.End.Position);
        }

        private static ITextBufferFactoryService GetTextBufferFactoryService()
        {
            var dllFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var projectFolder = Directory.GetParent(dllFolder).Parent.FullName;

            var dll1Name = "Microsoft.VisualStudio.Platform.VSEditor.dll";
            var dll2Name = "Microsoft.VisualStudio.Platform.VSEditor.Interop.dll";
            var dll1Path = Path.Combine(projectFolder, "Libraries", dll1Name);
            var dll2Path = Path.Combine(projectFolder, "Libraries", dll2Name);
            var outputDll1Path = Path.Combine(dllFolder, dll1Name);
            var outputDll2Path = Path.Combine(dllFolder, dll2Name);
            if (!File.Exists(outputDll1Path))
            {
                File.Copy(dll1Path, outputDll1Path, true);
            }

            if (!File.Exists(outputDll2Path))
            {
                File.Copy(dll2Path, outputDll2Path, true);
            }

            //Worked only with Microsoft.VisualStudio.Platform.VSEditor.Interop.dll in folder
            var assembly = Assembly.LoadFrom(Path.Combine(dllFolder, dll1Name));

            Assert.IsNotNull(assembly, "Assembly is null");
            var catalog = new AssemblyCatalog(assembly);
            Assert.IsNotNull(catalog, "Assembly catalog is null");
            var container = new CompositionContainer(catalog);
            Assert.IsNotNull(container, "Assembly container is null");
            var service = container.GetExportedValue<ITextBufferFactoryService>();
            Assert.IsNotNull(service, "TextBufferFactoryService is null");
            return service;
        }

        private static IEnumerable<Span> GetTextSnapshotSpan(ITextSnapshot textSnapshot)
        {
            yield return new Span(0, textSnapshot.Length);
        }
    }
}
