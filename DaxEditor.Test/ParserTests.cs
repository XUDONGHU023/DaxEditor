﻿// The project released under MS-PL license https://daxeditor.codeplex.com/license

using System;
using System.Linq;
using DaxEditor;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DaxEditorSample.Test
{
    /// <summary>
    /// Unut tests for parser
    /// </summary>
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void ParseQueryWithTrue()
        {
            Babel.Parser.Parser parser = ParseText(@"
DEFINE MEASURE Table1[M1] = CountRows ( Table1 )
EVALUATE CalculateTable (
   ADDCOLUMNS (
      FILTER(ALL(Table3),MOD([M1],2) = 1),
	  ""X"",
	  CALCULATE(CALCULATE([M1],AllSelected(Table3)))
   )
   , Table5[BOOL] = TRUE
   , Table3[IntMeasure] > 0
   , Table3[PK] > 4
)
");
            Assert.IsNotNull(parser);
        }

        [TestMethod]
        public void ParseQueryWithOrderBy()
        {
            Babel.Parser.Parser parser = ParseText(@"DEFINE 
	MEASURE Table1[PercentProfit]=SUMX(Table1,[SalesAmount] - [TotalProductCost]) 
	MEASURE DimProductSubCategory[CountProducts]=COUNTROWS(DimProduct) 
EVALUATE
	ADDCOLUMNS(
		FILTER(
			CROSSJOIN(
				VALUES(DimDate[CalendarYear]),
				VALUES(DimProductSubCategory[EnglishProductSubCategoryName])
			),
			NOT(ISBLANK(DimDate[CalendarYear])) && 	
			NOT(ISBLANK(DimProductSubCategory[EnglishProductSubCategoryName])) && 
			(
				NOT(ISBLANK(CALCULATE(SUM(Table1[SalesAmount])))) ||
				NOT(ISBLANK(Table1[PercentProfit])) ||
				NOT(ISBLANK(DimProductSubCategory[CountProducts]))
			)
		),
		""Amount"", CALCULATE(SUM(Table1[SalesAmount])),
		""PercentProfit"", Table1[PercentProfit],
		""CountProducts"", DimProductSubCategory[CountProducts]
	)	
ORDER BY 
	DimDate[CalendarYear] DESC,
	DimProductSubCategory[EnglishProductSubCategoryName] ASC
");
            Assert.IsNotNull(parser);
        }

        [TestMethod]
        public void ParseSimpleMeasure()
        {
            Babel.Parser.Parser parser = ParseText("CREATE MEASURE t[B]=Now()");

            Assert.AreEqual(1, parser.Measures.Count);
            var measure = parser.Measures.First();
            Assert.AreEqual("t", measure.TableName);
            Assert.AreEqual("B", measure.Name);
            Assert.AreEqual("Now()", measure.Expression);
            Assert.AreEqual("CREATE MEASURE t[B]=Now()", measure.FullText);
        }

        [TestMethod]
        public void ParseTrueFalse()
        {
            Babel.Parser.Parser parser = ParseText("CREATE MEASURE t[B]=TRUE() && TRUE || FALSE || FALSE()");

            Assert.AreEqual(1, parser.Measures.Count);
            var measure = parser.Measures.First();
            Assert.AreEqual("t", measure.TableName);
            Assert.AreEqual("B", measure.Name);
            Assert.AreEqual("TRUE() && TRUE || FALSE || FALSE()", measure.Expression);
            Assert.AreEqual("CREATE MEASURE t[B]=TRUE() && TRUE || FALSE || FALSE()", measure.FullText);
        }

        [TestMethod]
        public void ParseTI()
        {
            var parser = ParseText(@"CREATE MEASURE B[M1]=CALCULATE (
    [Net Value],
    DATEADD ( Calendar[Calendar Date], -1, MONTH )
)");

            Assert.AreEqual(1, parser.Measures.Count);
            var measure = parser.Measures.First();
            Assert.AreEqual("B", measure.TableName);
            Assert.AreEqual("M1", measure.Name);
            Assert.AreEqual(@"CALCULATE (
    [Net Value],
    DATEADD ( Calendar[Calendar Date], -1, MONTH )
)", measure.Expression);
        }

        [TestMethod]
        public void ParseTableNameTime()
        {
            Babel.Parser.Parser parser = ParseText("CREATE MEASURE 'Table1'[Hourly Avg CallCount]=AVERAGEX(CROSSJOIN(VALUES('Date'[DateID]), VALUES(Time[Hour])), [Count]);");

            Assert.AreEqual(1, parser.Measures.Count);
            var measure = parser.Measures.First();
            Assert.AreEqual("Table1", measure.TableName);
            Assert.AreEqual("Hourly Avg CallCount", measure.Name);
            Assert.AreEqual("AVERAGEX(CROSSJOIN(VALUES('Date'[DateID]), VALUES(Time[Hour])), [Count])", measure.Expression);
            Assert.AreEqual("CREATE MEASURE 'Table1'[Hourly Avg CallCount]=AVERAGEX(CROSSJOIN(VALUES('Date'[DateID]), VALUES(Time[Hour])), [Count])", measure.FullText);
        }

        [TestMethod]
        public void SeveralMeasures()
        {
            var text = @"CALCULATE; 
CREATE MEMBER CURRENTCUBE.Measures.[__XL_Count of Models] AS 1, VISIBLE = 0; 
ALTER CUBE CURRENTCUBE UPDATE DIMENSION Measures, Default_Member = [__XL_Count of Models]; 
----------------------------------------------------------
-- PowerPivot measures command (do not modify manually) --
----------------------------------------------------------


CREATE MEASURE Table1[Measure 1]=1;

----------------------------------------------------------
-- PowerPivot measures command (do not modify manually) --
----------------------------------------------------------


CREATE MEASURE 'Table1'[MeasureCountRows]=COUNTROWS(Table1);

";

            Babel.Parser.Parser parser = ParseText(text);

            Assert.AreEqual(2, parser.Measures.Count);
            var measure1 = parser.Measures.First();
            Assert.AreEqual("Table1", measure1.TableName);
            Assert.AreEqual("Measure 1", measure1.Name);
            Assert.AreEqual("1", measure1.Expression);
            Assert.AreEqual("CREATE MEASURE Table1[Measure 1]=1", measure1.FullText);
            var measure2 = parser.Measures.Skip(1).First();
            Assert.AreEqual("Table1", measure2.TableName);
            Assert.AreEqual("MeasureCountRows", measure2.Name);
            Assert.AreEqual("COUNTROWS(Table1)", measure2.Expression);
            Assert.AreEqual("CREATE MEASURE 'Table1'[MeasureCountRows]=COUNTROWS(Table1)", measure2.FullText);
        }

        [TestMethod]
        public void YearDayMonth()
        {
            var text = @" CREATE MEASURE 'TRANSACTIONS'[ThisYear]=Date(Year(Now()), Month(Now()), Day(Now()))";
            Babel.Parser.Parser parser = ParseText(text);
            Assert.AreEqual(1, parser.Measures.Count);
            var measure1 = parser.Measures.First();
            Assert.AreEqual("TRANSACTIONS", measure1.TableName);
            Assert.AreEqual("ThisYear", measure1.Name);
        }

        [TestMethod]
        public void CalculateShortcut()
        {
            var text = @" CREATE MEASURE 't3'[shortcut]=[M1](All(T)) + 't'[m 2](All(T2))";
            Babel.Parser.Parser parser = ParseText(text);
            Assert.AreEqual(1, parser.Measures.Count);
            var measure1 = parser.Measures.First();
            Assert.AreEqual("t3", measure1.TableName);
            Assert.AreEqual("shortcut", measure1.Name);
        }

        [TestMethod]
        public void ParseMeasureWithCalcProperty1()
        {
            var text = @"CREATE MEASURE 'Table1'[C]=1 CALCULATION PROPERTY NumberDecimal Accuracy=5 ThousandSeparator=True Format='#,0.00000'";

            Babel.Parser.Parser parser = ParseText(text);

            Assert.AreEqual(1, parser.Measures.Count);
            var measure1 = parser.Measures.First();
            Assert.AreEqual("Table1", measure1.TableName);
            Assert.AreEqual("C", measure1.Name);
            Assert.AreEqual("1", measure1.Expression);
            Assert.AreEqual("CREATE MEASURE 'Table1'[C]=1", measure1.FullText);
            Assert.IsNotNull(measure1.CalcProperty);
            Assert.AreEqual(DaxCalcProperty.FormatType.NumberDecimal, measure1.CalcProperty.Format);
            Assert.AreEqual("Member", measure1.CalcProperty.CalculationType);
            Assert.IsTrue(measure1.CalcProperty.Accuracy.HasValue);
            Assert.AreEqual(5, measure1.CalcProperty.Accuracy.Value);
        }

        [TestMethod]
        public void ParseMeasureCalcPropertyWrongFormatType()
        {
            var text = @"CREATE MEASURE 'Table1'[C]=1 CALCULATION PROPERTY WrongFormatType";
            try
            {
                Babel.Parser.Parser parser = ParseText(text);
                Assert.Fail("Exception expected");
            }
            catch (Exception e)
            {
                StringAssert.Contains(e.Message, "Wrong calculation property type");
            }
        }

        [TestMethod]
        public void ParseMultipleDaxQueries()
        {
            var text = @"EVALUATE t1 EVALUATE t2";
            Babel.Parser.Parser parser = ParseText(text);
            // Expect NOT to fail on parsing
        }

        [TestMethod]
        public void ParseKPI()
        {
            try
            {
                var text = @"CREATE KPI CURRENTCUBE.[Products with Negative Stock] AS Measures.[Products with Negative Stock], ASSOCIATED_MEASURE_GROUP = 'Product Inventory', GOAL = Measures.[_Products with Negative Stock Goal], STATUS = Measures.[_Products with Negative Stock Status], STATUS_GRAPHIC = 'Three Symbols UnCircled Colored';";
                Babel.Parser.Parser parser = ParseText(text);
                Assert.Fail("Exception expected");
            }
            catch (Exception e)
            {
                StringAssert.Contains(e.Message, "KPI are not yet supported");
            }
        }

        [TestMethod]
        public void ParseNumberThatStartsWithDot()
        {
            var text = @"EVALUATE ROW(""a"", .1)";
            Babel.Parser.Parser parser = ParseText(text);
        }

        [TestMethod]
        public void ParseFunctionWithoutParameterAfterComma()
        {
            try
            {
                var text = @"EVALUATE ROW(""a"", Calculate([m], ))";
                Babel.Parser.Parser parser = ParseText(text);
                Assert.Fail("Exception expected");
            }
            catch (Exception e)
            {
                StringAssert.Contains(e.Message, "syntax error");
            }
        }

        [TestMethod]
        public void ParseSimpleVarExpression()
        {
            var text = @"=
                VAR
                    CurrentSales = SUM ( Sales[Quantity] )
                VAR
                    SalesLastYear = CALCULATE (
                        SUM ( Sales[Quantity] ),
                        SAMEPERIODLASTYEAR ( 'Date'[Date] )
                    )
                RETURN
                    IF (
                        AND ( CurrentSales <> 0, SalesLastYear <> 0 ),
                        DIVIDE (
                            CurrentSales - SalesLastYear,
                            SalesLastYear
                        )
                    )";
            Babel.Parser.Parser parser = ParseText(text);
        }

        [TestMethod]
        public void ParseSimpleVarExpression2()
        {
            var text = @"=
                CALCULATETABLE (
                    ADDCOLUMNS (
                        VAR
                            OnePercentOfSales = [SalesAmount] * 0.01
                        RETURN
                            FILTER (
                                VALUES ( Product[Product Name] ),
                                [SalesAmount] >= OnePercentOfSales
                            ),
                        ""SalesOfProduct"", [SalesAmount]
                    ),
                    Product[Color] = ""Black""
                )";
            Babel.Parser.Parser parser = ParseText(text);
        }

        [TestMethod]
        public void ParseSimpleDataTable()
        {
            var text = @" = 
                DATATABLE (
                    ""Price Range"", STRING, 
                    ""Min Price"", CURRENCY, 
                    ""Max Price"", CURRENCY, 
                    {
                        { ""Low"", 0, 10 }, 
                        { ""Medium"", 10, 100 }, 
                        { ""High"", 100, 9999999 }
                    } 
                )";
            Babel.Parser.Parser parser = ParseText(text);
        }

        [TestMethod]
        public void ParseDifficultDataTable()
        {
            var text = @" = 
                DATATABLE (
                    ""Quarter"", STRING,
                    ""StartDate"", DATETIME,
                    ""EndDate"", DATETIME,
                    {
                        { ""Q1"", BLANK(), ""2015-03-31"" },
                        { ""Q2"", ""2015-04-01"", DATE(2009,4,15)+TIME(2,45,21) },
                        { ""Q3"",, ""2015-09-30"" },
                        { ""Q4"", ""2015-010-01"", ""2015-12-31"" }
                    }
                )";
            Babel.Parser.Parser parser = ParseText(text);
        }

        private static Babel.Parser.Parser ParseText(string text)
        {
            Babel.Parser.ErrorHandler handler = new Babel.Parser.ErrorHandler();
            Babel.Lexer.Scanner scanner = new Babel.Lexer.Scanner();
            Babel.Parser.Parser parser = new Babel.Parser.Parser();  // use noarg constructor
            parser.Trace = true;
            parser.scanner = scanner;
            scanner.Handler = handler;
            parser.SetHandler(handler);
            scanner.SetSourceText(text);

            var request = new ParseRequest(true);
            request.Sink = new AuthoringSink(ParseReason.None, 0, 0, Babel.Parser.Parser.MaxErrors);
            parser.MBWInit(request);
            var result = parser.Parse();
            if (handler.Errors)
                throw new Exception(handler.ToString());
            return parser;


        }

    }
}