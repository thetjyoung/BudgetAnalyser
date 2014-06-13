﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using BudgetAnalyser.Converters;
using BudgetAnalyser.Engine.Annotations;
using BudgetAnalyser.Engine.Budget;
using BudgetAnalyser.Engine.Ledger;

namespace BudgetAnalyser.LedgerBook
{
    public class LedgerBookGridBuilder : ILedgerBookGridBuilder
    {
        private const string BankBalanceBackground = "Brush.TileBackgroundAlternate";
        private const string BankBalanceTextBrush = "Brush.Text.Default";
        private const string DateFormat = "d-MMM-yy";
        private const string HeadingStyle = "LedgerBookTextBlockHeading";
        private const string ImportantNumberStyle = "LedgerBookTextBlockImportantNumber";
        private const string LightBorderBrush = "Brush.BorderLight";

        private const string NormalHighlightBackground = "Brush.TileBackground";
        private const string NormalStyle = "LedgerBookTextBlockOther";
        private const string NumberStyle = "LedgerBookTextBlockNumber";
        private const string SurplusBackground = "Brush.TileBackgroundAlternate";
        private const string SurplusTextBrush = "Brush.CreditBackground1";
        private static readonly GridLength MediumGridWidth = new GridLength(100);
        private static readonly GridLength SmallGridWidth = new GridLength(60);
        private readonly ICommand removeLedgerEntryLineCommand;
        private readonly ICommand showBankBalancesCommand;
        private readonly ICommand showRemarksCommand;
        private readonly ICommand showTransactionsCommand;
        private ContentPresenter content;
        private Engine.Ledger.LedgerBook ledgerBook;
        private ResourceDictionary localResources;

        public LedgerBookGridBuilder(
            ICommand showTransactionsCommand,
            ICommand showBankBalancesCommand,
            ICommand showRemarksCommand,
            ICommand removeLedgerEntryLineCommand)
        {
            this.showTransactionsCommand = showTransactionsCommand;
            this.showBankBalancesCommand = showBankBalancesCommand;
            this.showRemarksCommand = showRemarksCommand;
            this.removeLedgerEntryLineCommand = removeLedgerEntryLineCommand;
        }

        /// <summary>
        ///     This is drawn programatically because the dimensions of the ledger book grid are two-dimensional and dynamic.
        ///     Unknown number
        ///     of columns and many rows. ListView and DataGrid dont work well.
        /// </summary>
        public void BuildGrid([CanBeNull] Engine.Ledger.LedgerBook currentLedgerBook, [NotNull] ResourceDictionary viewResources, [NotNull] ContentPresenter contentPanel)
        {
            if (viewResources == null)
            {
                throw new ArgumentNullException("viewResources");
            }

            if (contentPanel == null)
            {
                throw new ArgumentNullException("contentPanel");
            }

            this.ledgerBook = currentLedgerBook;
            this.localResources = viewResources;
            this.content = contentPanel;
            DynamicallyCreateLedgerBookGrid();
        }

        private Border AddBorderToGridCell(Panel parent, bool hasBackground, bool hasBorder, int column, int row)
        {
            return AddBorderToGridCell(parent, hasBackground ? NormalHighlightBackground : null, hasBorder, column, row);
        }

        private Border AddBorderToGridCell(Panel parent, string background, bool hasBorder, int column, int row)
        {
            var border = new Border();
            if (background != null)
            {
                border.Background = FindResource(background) as Brush;
            }

            if (hasBorder)
            {
                border.BorderBrush = FindResource(LightBorderBrush) as Brush;
                border.BorderThickness = new Thickness(0, 0, 1, 0);
            }

            Grid.SetColumn(border, column);
            Grid.SetRow(border, row);
            parent.Children.Add(border);
            return border;
        }

        private TextBlock AddContentToGrid(FrameworkElement parent, string content, ref int column, int row, string style, string tooltip = null)
        {
            var panel = parent as Panel;
            var decorator = parent as Decorator;

            var textBlock = new TextBlock
            {
                Style = FindResource(style) as Style,
                Text = content,
                ToolTip = tooltip ?? content,
            };
            Grid.SetColumn(textBlock, column++);
            Grid.SetRow(textBlock, row);
            if (panel != null)
            {
                panel.Children.Add(textBlock);
            }
            else if (decorator != null)
            {
                decorator.Child = textBlock;
            }
            else
            {
                throw new ArgumentException("parent is not a Panel nor a Decorator", "parent");
            }

            return textBlock;
        }

        private int AddDateCellToLedgerEntryLine(Grid grid, int column, int row, LedgerEntryLine line)
        {
            Border dateBorder = AddBorderToGridCell(grid, false, true, column, row);
            AddContentToGrid(dateBorder, line.Date.ToString(DateFormat, CultureInfo.CurrentCulture), ref column, row, NormalStyle);
            column--; // Not finished adding content to this cell yet.
            var button = new Button
            {
                Style = Application.Current.Resources["Button.Round.SmallCross"] as Style,
                HorizontalAlignment = HorizontalAlignment.Right,
                Command = this.removeLedgerEntryLineCommand,
                CommandParameter = line,
            };
            var visibilityBinding = new Binding("IsEnabled")
            {
                Converter = (IValueConverter)Application.Current.Resources["Converter.BoolToVis"],
                RelativeSource = new RelativeSource(RelativeSourceMode.Self),
            };
            button.SetBinding(UIElement.VisibilityProperty, visibilityBinding);

            grid.Children.Add(button);
            Grid.SetColumn(button, column++);
            Grid.SetRow(button, row);

            return column;
        }

        private void AddHeadingRow(Grid grid)
        {
            int column = 0;
            Border dateBorder = AddBorderToGridCell(grid, true, true, column, 0);
            AddContentToGrid(dateBorder, "Date", ref column, 0, HeadingStyle);

            foreach (LedgerColumn ledger in this.ledgerBook.Ledgers)
            {
                Border border = AddBorderToGridCell(grid, true, true, column, 0);
                // SpentMonthly Legders do not show the transaction total (NetAmount) because its always the same.
                Grid.SetColumnSpan(border, ledger.BudgetBucket is SpentMonthlyExpenseBucket ? 1 : 2);

                // Heading stripe to indicate SpentMonthly or SavedUpFor expenses.
                var stripe = new Border
                {
                    BorderThickness = new Thickness(0, 6, 0, 0),
                    BorderBrush = ledger.BudgetBucket is SpentMonthlyExpenseBucket ? ConverterHelper.SpentMonthlyBucketBrush : ConverterHelper.AccumulatedBucketBrush,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top,
                };
                border.Child = stripe;

                string tooltip = string.Format(CultureInfo.CurrentCulture, "{0}: {1} - {2}", ledger.BudgetBucket.TypeDescription, ledger.BudgetBucket.Code, ledger.BudgetBucket.Description);
                TextBlock ledgerTitle = AddContentToGrid(stripe, ledger.BudgetBucket.Code, ref column, 0, HeadingStyle, tooltip);
                ledgerTitle.HorizontalAlignment = HorizontalAlignment.Center;
                column--; // Ledger heading shares a column with other Ledger Headings

                if (!(ledger.BudgetBucket is SpentMonthlyExpenseBucket))
                {
                    TextBlock ledgerTxnsHeading = AddContentToGrid(grid, "Txns", ref column, 0, HeadingStyle, "Transactions");
                    ledgerTxnsHeading.Margin = new Thickness(5, 30, 5, 2);
                    ledgerTxnsHeading.HorizontalAlignment = HorizontalAlignment.Right;
                }

                TextBlock ledgerBalanceHeading = AddContentToGrid(grid, "Balance", ref column, 0, HeadingStyle, "Ledger Balance");
                ledgerBalanceHeading.Margin = new Thickness(5, 30, 5, 2);
                ledgerBalanceHeading.HorizontalAlignment = HorizontalAlignment.Right;
            }

            Border surplusBorder = AddBorderToGridCell(grid, SurplusBackground, false, column, 0);
            TextBlock surplusTextBlock = AddContentToGrid(surplusBorder, "Surplus", ref column, 0, HeadingStyle);
            surplusTextBlock.Foreground = FindResource(SurplusTextBrush) as Brush;
            surplusTextBlock.HorizontalAlignment = HorizontalAlignment.Right;

            Border adjustmentsBorder = AddBorderToGridCell(grid, true, false, column, 0);
            TextBlock adjustmentsTextBlock = AddContentToGrid(adjustmentsBorder, "Adjustments", ref column, 0, HeadingStyle);
            adjustmentsTextBlock.HorizontalAlignment = HorizontalAlignment.Right;

            Border bankBalanceBorder = AddBorderToGridCell(grid, BankBalanceBackground, false, column, 0);
            TextBlock bankBalanceTextBlock = AddContentToGrid(bankBalanceBorder, "Balance", ref column, 0, HeadingStyle);
            bankBalanceTextBlock.Foreground = FindResource(BankBalanceTextBrush) as Brush;
            bankBalanceTextBlock.HorizontalAlignment = HorizontalAlignment.Right;

            AddBorderToGridCell(grid, true, false, column, 0);
            AddContentToGrid(grid, "Remarks", ref column, 0, HeadingStyle);
        }

        private TextBlock AddHyperlinkToGrid(Panel parent, string content, ref int column, int row, string style, string tooltip = null, object parameter = null)
        {
            var hyperlink = new Hyperlink(new Run(content))
            {
                Command = this.showTransactionsCommand,
                CommandParameter = parameter,
            };
            var textBlock = new TextBlock(hyperlink)
            {
                Style = FindResource(style) as Style,
                ToolTip = tooltip ?? content,
            };
            Grid.SetColumn(textBlock, column++);
            Grid.SetRow(textBlock, row);
            parent.Children.Add(textBlock);
            return textBlock;
        }

        private void AddLedgerColumns(Grid grid)
        {
            foreach (LedgerColumn ledger in this.ledgerBook.Ledgers)
            {
                if (ledger.BudgetBucket is SpentMonthlyExpenseBucket)
                {
                    // Spent Monthly ledgers only have a balance column
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = SmallGridWidth });
                }
                else
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = SmallGridWidth });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = SmallGridWidth });
                }
            }
        }

        private void AddLedgerEntryLines(Grid grid)
        {
            int row = 1;
            List<LedgerColumn> allLedgers = this.ledgerBook.Ledgers.ToList();
            foreach (LedgerEntryLine line in this.ledgerBook.DatedEntries)
            {
                int column = 0;
                column = AddDateCellToLedgerEntryLine(grid, column, row, line);

                foreach (LedgerColumn ledger in allLedgers)
                {
                    LedgerEntry entry = line.Entries.FirstOrDefault(e => e.LedgerColumn.Equals(ledger));
                    decimal balance, netAmount;

                    if (entry == null)
                    {
                        // New ledger added that older entries do not have.
                        balance = 0;
                        netAmount = 0;
                    }
                    else
                    {
                        balance = entry.Balance;
                        netAmount = entry.NetAmount;
                    }

                    if (ledger.BudgetBucket is SpentMonthlyExpenseBucket)
                    {
                        AddBorderToGridCell(grid, false, true, column, row);
                        AddHyperlinkToGrid(grid, balance.ToString("N", CultureInfo.CurrentCulture), ref column, row, NumberStyle, parameter: entry);
                    }
                    else
                    {
                        AddBorderToGridCell(grid, true, false, column, row);
                        AddHyperlinkToGrid(grid, netAmount.ToString("N", CultureInfo.CurrentCulture), ref column, row, NumberStyle, parameter: entry);
                        AddBorderToGridCell(grid, false, true, column, row);
                        AddHyperlinkToGrid(grid, balance.ToString("N", CultureInfo.CurrentCulture), ref column, row, NumberStyle, parameter: entry);
                    }
                }

                Border surplusBorder = AddBorderToGridCell(grid, SurplusBackground, false, column, row);
                TextBlock surplusText = AddContentToGrid(surplusBorder, line.CalculatedSurplus.ToString("N", CultureInfo.CurrentCulture), ref column, row, ImportantNumberStyle);
                surplusText.Foreground = FindResource(SurplusTextBrush) as Brush;

                AddHyperlinkToGrid(
                    grid,
                    line.TotalBalanceAdjustments.ToString("N", CultureInfo.CurrentCulture),
                    ref column,
                    row,
                    ImportantNumberStyle,
                    parameter: line);

                AddBorderToGridCell(grid, BankBalanceBackground, false, column, row);
                TextBlock bankBalanceText = AddHyperlinkToGrid(
                    grid,
                    line.LedgerBalance.ToString("N", CultureInfo.CurrentCulture),
                    ref column,
                    row,
                    ImportantNumberStyle,
                    string.Format(CultureInfo.CurrentCulture, "Ledger Balance: {0:N} Bank Balance {1:N}", line.LedgerBalance, line.TotalBankBalance),
                    line);
                var hyperlink = (Hyperlink)bankBalanceText.Inlines.FirstInline;
                hyperlink.Command = this.showBankBalancesCommand;
                hyperlink.CommandParameter = line;
                bankBalanceText.Foreground = FindResource(BankBalanceTextBrush) as Brush;

                TextBlock remarksHyperlink = AddHyperlinkToGrid(grid, "...", ref column, row, NormalStyle, line.Remarks);
                hyperlink = (Hyperlink)remarksHyperlink.Inlines.FirstInline;
                hyperlink.Command = this.showRemarksCommand;
                hyperlink.CommandParameter = line;

                row++;
            }
        }

        private void AddLedgerRows(Grid grid)
        {
            for (int index = 0; index <= this.ledgerBook.DatedEntries.Count(); index++)
            {
                // <= because we must allow one row for the heading.
                if (index >= 12)
                {
                    // Maximum of 12 months + 1 heading row
                    break;
                }

                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
        }

        private void DynamicallyCreateLedgerBookGrid()
        {
            if (this.ledgerBook == null)
            {
                this.content = null;
                return;
            }

            var grid = new Grid();
            // Date
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = MediumGridWidth });
            // Ledgers 
            AddLedgerColumns(grid);
            // Surplus
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = MediumGridWidth });
            // Adjustments
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = MediumGridWidth });
            // Ledger Balance
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = MediumGridWidth });
            // Remarks
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = SmallGridWidth });

            this.content.Content = grid;
            AddLedgerRows(grid);
            AddHeadingRow(grid);
            AddLedgerEntryLines(grid);
        }

        private object FindResource(string resourceName)
        {
            object localResource = this.localResources[resourceName];
            if (localResource != null)
            {
                return localResource;
            }

            return Application.Current.FindResource(resourceName);
        }
    }
}