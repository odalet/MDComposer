////// Copyright (c) 2019 AlphaSierraPapa for the SharpDevelop Team
////// 
////// Permission is hereby granted, free of charge, to any person obtaining a copy of this
////// software and associated documentation files (the "Software"), to deal in the Software
////// without restriction, including without limitation the rights to use, copy, modify, merge,
////// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
////// to whom the Software is furnished to do so, subject to the following conditions:
////// 
////// The above copyright notice and this permission notice shall be included in all copies or
////// substantial portions of the Software.
////// 
////// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
////// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
////// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
////// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
////// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
////// DEALINGS IN THE SOFTWARE.

////// Adapted from https://raw.githubusercontent.com/icsharpcode/WpfDesigner/0ff33b17b047bc01b0ea669592614c4d450ddda2/WpfDesign.XamlDom/Project/PositionXmlDocument.cs

////using System;
////using System.Diagnostics.CodeAnalysis;
////using System.Linq.Expressions;
////using System.Reflection;
////using System.Text;
////using System.Xml;

////namespace Delta.MDComposer.Utils;

////[SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface")]
////[SuppressMessage("Microsoft.Design", "CA1058:TypesShouldNotExtendCertainBaseTypes")]
////[SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
////public class PositionXmlDocument : XmlDocument
////{
////    private IXmlLineInfo? lineInfo;
    
////    // a reference to the XmlReader, only set during load time
////    internal XmlWriter? oldWriter;
////    internal StringBuilder? writerBuilder;
////    internal Func<char[]>? getBuffer;
////    internal Func<int>? getContentPositionField;
////    internal Func<int>? getBufferPosition;
////    internal int lastCharacterPosition = 0;
////    internal int previousLinePosition = 0;
////    internal int lineCount = 0;

////    public override XmlElement CreateElement(string? prefix, string localName, string? namespaceURI) =>
////        new PositionXmlElement(prefix, localName, namespaceURI, this, lineInfo);

////    /// <summary>
////    /// Creates a PositionXmlAttribute.
////    /// </summary>
////    public override XmlAttribute CreateAttribute(string? prefix, string localName, string? namespaceURI) =>
////        new PositionXmlAttribute(prefix, localName, namespaceURI, this, lineInfo);

////    /// <summary>
////    /// Loads the XML document from the specified <see cref="XmlReader"/>.
////    /// </summary>
////    public override void Load(XmlReader reader)
////    {
////        lineInfo = reader as IXmlLineInfo;
////        try
////        {
////            base.Load(reader);
////        }
////        finally
////        {
////            lineInfo = null;
////        }
////    }
////}

////[SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
////public class PositionXmlElement : XmlElement, IXmlLineInfo
////{
////    private sealed class XamlElementLineInfo(int lineNumber, int linePosition)
////    {
////        public int LineNumber { get; set; } = lineNumber;
////        public int LinePosition { get; set; } = linePosition;
////    }

////    private readonly bool hasLineInfo;
////    private readonly PositionXmlDocument positionXmlDocument;
////    private XamlElementLineInfo? xamlElementLineInfo;

////    internal PositionXmlElement(
////        string? prefix, string localName, string? namespaceURI, PositionXmlDocument doc, IXmlLineInfo? lineInfo)
////        : base(prefix, localName, namespaceURI, doc)
////    {
////        if (lineInfo != null)
////            xamlElementLineInfo = new XamlElementLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);

////        positionXmlDocument = doc;
////    }

////    public bool HasLineInfo() => xamlElementLineInfo != null;

////    public int LineNumber => xamlElementLineInfo?.LineNumber ?? -1;
////    public int LinePosition => xamlElementLineInfo?.LinePosition ?? -1;
////    ////public XamlElementLineInfo XamlElementLineInfo => xamlElementLineInfo;

////    public override void WriteTo(XmlWriter w)
////    {
////        if (positionXmlDocument.oldWriter != w)
////        {
////            try
////            {
////                positionXmlDocument.oldWriter = w;
////                positionXmlDocument.lineCount = 0;
////                positionXmlDocument.previousLinePosition = 0;

////                var xmlWriterField = w.GetType().GetField(
////                    "xmlWriter", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

////                if (xmlWriterField != null)
////                {
////                    var xmlwriter = xmlWriterField.GetValue(w);
////                    var rawTextWPrp = xmlwriter?.GetType().GetProperty(
////                        "InnerWriter", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

////                    if (rawTextWPrp != null)
////                    {
////                        var rawTextW = rawTextWPrp.GetValue(xmlwriter, null);
////                        var bufCharsField = rawTextW.GetType().GetField(
////                            "bufChars", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
////                        var contentPosField = rawTextW.GetType().GetField(
////                            "contentPos", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
////                        var buffPosField = rawTextW.GetType().GetField(
////                            "bufPos", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
////                        var ioTextWriterField = rawTextW.GetType().GetField(
////                            "writer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
////                        var ioTextWriter = ioTextWriterField.GetValue(rawTextW);
////                        var sbField = ioTextWriter.GetType().GetField("_sb", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
////                        positionXmlDocument.writerBuilder = sbField.GetValue(ioTextWriter) as StringBuilder;

////                        positionXmlDocument.getBuffer =
////                            Expression.Lambda<Func<char[]>>(Expression.Field(Expression.Constant(rawTextW), bufCharsField)).Compile();
////                        positionXmlDocument.getContentPositionField =
////                            Expression.Lambda<Func<int>>(Expression.Field(Expression.Constant(rawTextW), contentPosField)).Compile();
////                        positionXmlDocument.getBufferPosition =
////                            Expression.Lambda<Func<int>>(Expression.Field(Expression.Constant(rawTextW), buffPosField)).Compile();
////                    }
////                    else
////                    {
////                        rawTextWPrp = xmlwriter.GetType()
////                            .GetProperty("RawWriter", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
////                        if (rawTextWPrp != null)
////                        {
////                            var rawTextW = rawTextWPrp.GetValue(xmlwriter, null);
////                            var bufCharsField = rawTextW.GetType()
////                                .GetField("_bufChars", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
////                            var contentPosField = rawTextW.GetType()
////                                .GetField("_contentPos", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
////                            var buffPosField = rawTextW.GetType()
////                                .GetField("_bufPos", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
////                            var ioTextWriterField = rawTextW.GetType()
////                                .GetField("_writer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
////                            var ioTextWriter = ioTextWriterField.GetValue(rawTextW);
////                            var sbField = ioTextWriter.GetType()
////                                .GetField("_sb", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
////                            positionXmlDocument.writerBuilder = sbField.GetValue(ioTextWriter) as StringBuilder;

////                            positionXmlDocument.getBuffer =
////                                Expression.Lambda<Func<char[]>>(Expression.Field(Expression.Constant(rawTextW), bufCharsField)).Compile();
////                            positionXmlDocument.getContentPositionField =
////                                Expression.Lambda<Func<int>>(Expression.Field(Expression.Constant(rawTextW), contentPosField)).Compile();
////                            positionXmlDocument.getBufferPosition =
////                                Expression.Lambda<Func<int>>(Expression.Field(Expression.Constant(rawTextW), buffPosField)).Compile();
////                        }
////                    }

////                }
////            }
////            catch (Exception)
////            { }
////        }

////        if (positionXmlDocument.getBuffer != null && positionXmlDocument.getBufferPosition != null &&
////            positionXmlDocument.writerBuilder != null)
////        {
////            try
////            {
////                var buff = positionXmlDocument.getBuffer();
////                var pos = positionXmlDocument.getBufferPosition();
////                for (int n = pos; n >= positionXmlDocument.lastCharacterPosition; n--)
////                {
////                    if (buff[n] == '\n')
////                    {
////                        positionXmlDocument.lineCount++;
////                    }
////                }

////                this.xamlElementLineInfo = new XamlElementLineInfo(positionXmlDocument.lineCount + 1,
////                    pos + 1 + positionXmlDocument.writerBuilder.Length);

////                if (buff[pos - 1] != '>')
////                    this.xamlElementLineInfo.LinePosition++;

////                this.xamlElementLineInfo.Position = pos + positionXmlDocument.writerBuilder.Length;
////            }
////            catch (Exception)
////            {
////            }
////        }

////        base.WriteTo(w);

////        if (positionXmlDocument.getBuffer != null && positionXmlDocument.getBufferPosition != null &&
////            positionXmlDocument.writerBuilder != null)
////        {
////            try
////            {
////                var pos = positionXmlDocument.getBufferPosition();
////                xamlElementLineInfo.Length = pos + positionXmlDocument.writerBuilder.Length - this.xamlElementLineInfo.Position;
////            }
////            catch (Exception)
////            {
////            }
////        }
////    }
////}

////[SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface")]
////public sealed class PositionXmlAttribute : XmlAttribute, IXmlLineInfo
////{
////    private readonly bool hasLineInfo;

////    internal PositionXmlAttribute(
////        string? prefix, string localName, string? namespaceURI, XmlDocument doc, IXmlLineInfo? lineInfo)
////        : base(prefix, localName, namespaceURI, doc)
////    {
////        if (lineInfo == null) return;

////        LineNumber = lineInfo.LineNumber;
////        LinePosition = lineInfo.LinePosition;
////        hasLineInfo = true;
////    }

////    public int LineNumber { get; }
////    public int LinePosition { get; }

////    public bool HasLineInfo() => hasLineInfo;
////}