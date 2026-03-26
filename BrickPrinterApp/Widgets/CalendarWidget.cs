using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Models;
using BrickPrinterApp.Services;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Meadow.Foundation.Graphics;
using Meadow.Foundation.Graphics.Buffers;
using Meadow.Peripherals.Displays;
using Color = Meadow.Color;

namespace BrickPrinterApp.Widgets;

public class CalendarWidget : IWidget
{
    private readonly GoogleAuthService _googleAuth;
    private readonly Buffer1bpp _buffer;
    private readonly MicroGraphics _graphics;
    private readonly IFont _font;
    private readonly object _lock = new();

    private List<CalendarEvent> _events = new();
    private DateTime _lastFetch = DateTime.MinValue;
    private bool _showDailyView = false;
    private const int ScreenWidth = 128;
    private const int ScreenHeight = 64;
    private const int RowHeight = 8;
    private const int DaysToFetch = 7;

    public string Name => "Calendar";
    public TimeSpan UpdateInterval => TimeSpan.FromSeconds(30);

    public CalendarWidget(GoogleAuthService googleAuth)
    {
        _googleAuth = googleAuth;
        _buffer = new Buffer1bpp(ScreenWidth, ScreenHeight);
        var displaySimulator = new DisplayBufferSimulator(_buffer);
        _font = new Font4x8();
        _graphics = new MicroGraphics(displaySimulator)
        {
            CurrentFont = _font
        };
    }

    public byte[] GetContent()
    {
        Console.WriteLine($"Calendar: GetContent called");
        lock (_lock)
        {
            // Fetch new events every 5 minutes
            if ((DateTime.Now - _lastFetch).TotalMinutes > 5)
            {
                Console.WriteLine("Calendar: Time to fetch events");
                FetchUpcomingEvents();
                _lastFetch = DateTime.Now;
            }

            // Clear and set font
            _buffer.Clear();
            _graphics.Clear(true);
            _graphics.CurrentFont = _font;

            // Check if connected
            if (_googleAuth.Status != GoogleAuthStatus.Connected)
            {
                DrawNotConnectedScreen();
            }
            else
            {
                var now = DateTime.Now;

                // Find current event (if any)
                var currentEvent = _events
                    .Where(e => !e.IsAllDay && e.Start <= now && e.End > now)
                    .FirstOrDefault();

                // Find next upcoming event
                var nextEvent = _events
                    .Where(e => !e.IsAllDay && e.Start > now)
                    .OrderBy(e => e.Start)
                    .FirstOrDefault();

                if (currentEvent != null)
                {
                    DrawCurrentEventScreen(currentEvent, nextEvent);
                }
                else if (_showDailyView)
                {
                    DrawUpcomingEventsScreen();
                }
                else
                {
                    DrawNextEventScreen(nextEvent);
                }

                // Toggle view for next update (only when no current event)
                if (currentEvent == null)
                {
                    _showDailyView = !_showDailyView;
                }
            }

            _graphics.Show();

            // Return a copy of the buffer
            var result = new byte[_buffer.Buffer.Length];
            Array.Copy(_buffer.Buffer, result, result.Length);
            return result;
        }
    }

    private void FetchUpcomingEvents()
    {
        Console.WriteLine($"Calendar: FetchUpcomingEvents called, status = {_googleAuth.Status}");

        if (_googleAuth.Status != GoogleAuthStatus.Connected)
        {
            Console.WriteLine("Calendar: Not connected, skipping fetch");
            return;
        }

        try
        {
            var credential = _googleAuth.GetCredential();
            if (credential == null)
            {
                Console.WriteLine("Calendar: Credential is null");
                return;
            }

            Console.WriteLine("Calendar: Fetching from Google API...");

            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "BrickPrinter"
            });

            var today = DateTime.Today;
            var endDate = today.AddDays(DaysToFetch);

            _events.Clear();

            // Get list of all calendars
            var calendarsRequest = service.CalendarList.List();
            var calendars = calendarsRequest.Execute();

            foreach (var calendar in calendars.Items)
            {
                try
                {
                    var request = service.Events.List(calendar.Id);
                    request.TimeMinDateTimeOffset = new DateTimeOffset(today);
                    request.TimeMaxDateTimeOffset = new DateTimeOffset(endDate);
                    request.SingleEvents = true;
                    request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                    var events = request.Execute();

                    if (events.Items != null)
                    {
                        foreach (var evt in events.Items)
                        {
                            var calEvent = new CalendarEvent
                            {
                                Title = evt.Summary ?? "(Kein Titel)",
                                IsAllDay = evt.Start.Date != null
                            };

                            if (calEvent.IsAllDay)
                            {
                                calEvent.Start = DateTime.Parse(evt.Start.Date ?? DateTime.Today.ToString("yyyy-MM-dd"));
                                calEvent.End = DateTime.Parse(evt.End.Date ?? DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"));
                            }
                            else
                            {
                                calEvent.Start = evt.Start.DateTimeDateTimeOffset?.LocalDateTime ?? DateTime.Now;
                                calEvent.End = evt.End.DateTimeDateTimeOffset?.LocalDateTime ?? DateTime.Now;
                            }

                            _events.Add(calEvent);
                        }
                    }
                }
                catch
                {
                    // Skip calendars we can't access
                }
            }

            // Sort all events by start time
            _events = _events.OrderBy(e => e.Start).ToList();

            Console.WriteLine($"Calendar: Fetched {_events.Count} events from {calendars.Items.Count} calendars for next {DaysToFetch} days");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Calendar fetch failed: {ex.Message}");
            Console.WriteLine($"Calendar exception: {ex}");
        }
    }

    private void DrawNotConnectedScreen()
    {
        DrawTextRow(0, "KALENDER", true);
        DrawTextRow(2, "Nicht verbunden", false);
        DrawTextRow(3, "Einstellungen ->", false);
        DrawTextRow(4, "Google Tab", false);
    }

    private void DrawCurrentEventScreen(CalendarEvent current, CalendarEvent? next)
    {
        // Header
        DrawTextRow(0, ">>> JETZT <<<", true);

        // Current event title
        var title = TruncateText(current.Title, 32);
        DrawTextRow(1, title, false);

        // Time remaining
        var remaining = current.End - DateTime.Now;
        var remainingText = remaining.TotalMinutes < 60
            ? $"Noch {(int)remaining.TotalMinutes} min"
            : $"Noch {(int)remaining.TotalHours}h {remaining.Minutes}m";
        DrawTextRow(2, remainingText, false);

        // End time
        DrawTextRow(3, $"Ende: {current.End:HH:mm}", true);

        // Show next event if exists
        if (next != null)
        {
            var isToday = next.Start.Date == DateTime.Today;
            var datePrefix = isToday ? "" : $"{next.Start:dd.MM} ";
            DrawTextRow(5, "Danach:", true);
            var nextTitle = TruncateText(next.Title, 20);
            DrawTextRow(6, $"{datePrefix}{next.Start:HH:mm} {nextTitle}", false);
        }
    }

    private void DrawNextEventScreen(CalendarEvent? next)
    {
        // Header with date
        DrawTextRow(0, DateTime.Now.ToString("ddd dd.MM.yyyy"), true);

        if (next == null)
        {
            DrawTextRow(2, "Keine Termine", false);
            DrawTextRow(3, "in den naechsten", false);
            DrawTextRow(4, $"{DaysToFetch} Tagen", false);
        }
        else
        {
            var isToday = next.Start.Date == DateTime.Today;
            var isTomorrow = next.Start.Date == DateTime.Today.AddDays(1);

            // Show when
            string whenText;
            if (isToday)
            {
                var until = next.Start - DateTime.Now;
                if (until.TotalMinutes < 60)
                    whenText = $"in {(int)until.TotalMinutes} min";
                else if (until.TotalHours < 2)
                    whenText = $"in {(int)until.TotalHours}h {until.Minutes}m";
                else
                    whenText = $"in {(int)until.TotalHours} Stunden";
            }
            else if (isTomorrow)
            {
                whenText = $"Morgen {next.Start:HH:mm}";
            }
            else
            {
                whenText = next.Start.ToString("ddd dd.MM HH:mm");
            }

            DrawTextRow(1, "NAECHSTER TERMIN", true);
            DrawTextRow(2, whenText, false);

            // Event time (only show duration for today)
            if (isToday)
            {
                DrawTextRow(3, $"{next.Start:HH:mm} - {next.End:HH:mm}", false);
            }

            // Event title
            DrawTextRow(5, TruncateText(next.Title, 32), true);
        }
    }

    private void DrawUpcomingEventsScreen()
    {
        var now = DateTime.Now;

        // Get upcoming events (not past, not currently running)
        var upcomingEvents = _events
            .Where(e => e.Start > now || (e.IsAllDay && e.Start.Date >= DateTime.Today))
            .OrderBy(e => e.Start)
            .Take(10)
            .ToList();

        if (!upcomingEvents.Any())
        {
            DrawTextRow(0, "KALENDER", true);
            DrawTextRow(2, "Keine Termine", false);
            DrawTextRow(3, "geplant", false);
            return;
        }

        int currentRow = 0;
        DateTime? lastDate = null;

        foreach (var evt in upcomingEvents)
        {
            if (currentRow >= 8) break;

            var eventDate = evt.Start.Date;

            // Add date header if date changes
            if (lastDate != eventDate)
            {
                if (currentRow >= 7) break; // Need space for date + at least one event

                var dateText = GetDateHeader(eventDate);
                DrawTextRow(currentRow, dateText, true);
                currentRow++;
                lastDate = eventDate;
            }

            if (currentRow >= 8) break;

            // Draw event
            string eventLine;
            if (evt.IsAllDay)
            {
                eventLine = $"[*] {TruncateText(evt.Title, 28)}";
            }
            else
            {
                eventLine = $"{evt.Start:HH:mm} {TruncateText(evt.Title, 26)}";
            }
            DrawTextRow(currentRow, eventLine, false);
            currentRow++;
        }
    }

    private string GetDateHeader(DateTime date)
    {
        if (date == DateTime.Today)
            return $"HEUTE {date:dd.MM}";
        if (date == DateTime.Today.AddDays(1))
            return $"MORGEN {date:dd.MM}";
        return date.ToString("ddd dd.MM");
    }

    private void DrawTextRow(int rowIndex, string text, bool inverted)
    {
        int y = rowIndex * RowHeight;

        if (inverted)
        {
            // Draw white filled rectangle for background
            _graphics.DrawRectangle(0, y, ScreenWidth, RowHeight, Color.White, true);
            // Draw black text on white background
            _graphics.DrawText(1, y, text, Color.Black);
        }
        else
        {
            // Draw white text on black background
            _graphics.DrawText(1, y, text, Color.White);
        }
    }

    private string TruncateText(string text, int maxChars)
    {
        if (text.Length <= maxChars) return text;
        return text.Substring(0, maxChars - 2) + "..";
    }

    private class CalendarEvent
    {
        public string Title { get; set; } = "";
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool IsAllDay { get; set; }
    }
}
