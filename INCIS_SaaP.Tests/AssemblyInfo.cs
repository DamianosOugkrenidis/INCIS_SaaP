using Xunit;

// Απενεργοποίηση παράλληλης εκτέλεσης: αποφυγή race conditions όταν πολλαπλά
// WebApplicationFactory instances εκκινούν ταυτόχρονα (shared static state:
// QuestPDF license, Serilog, ML model files, health checks).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
