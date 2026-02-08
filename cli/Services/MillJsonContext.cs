using System.Text.Json.Serialization;
using Mill.Models;

namespace Mill.Services;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
// Domain models
[JsonSerializable(typeof(KnowledgeItem))]
[JsonSerializable(typeof(List<KnowledgeItem>))]
[JsonSerializable(typeof(Draft))]
[JsonSerializable(typeof(List<Draft>))]
[JsonSerializable(typeof(DraftDetail))]
[JsonSerializable(typeof(DraftPublishResult))]
[JsonSerializable(typeof(DraftValidationResult))]
[JsonSerializable(typeof(Idea))]
[JsonSerializable(typeof(List<Idea>))]
[JsonSerializable(typeof(IdeaDetail))]
[JsonSerializable(typeof(DroppedIdea))]
[JsonSerializable(typeof(List<DroppedIdea>))]
[JsonSerializable(typeof(DroppedIdeasFile))]
[JsonSerializable(typeof(Observation))]
[JsonSerializable(typeof(ObservationDetail))]
[JsonSerializable(typeof(ObservationsList))]
[JsonSerializable(typeof(Spec))]
[JsonSerializable(typeof(List<Spec>))]
[JsonSerializable(typeof(SpecDetail))]
[JsonSerializable(typeof(HistoryEntry))]
[JsonSerializable(typeof(List<HistoryEntry>))]
[JsonSerializable(typeof(HistoryFile))]
[JsonSerializable(typeof(TemplateSummary))]
[JsonSerializable(typeof(List<TemplateSummary>))]
[JsonSerializable(typeof(TemplateContent))]
[JsonSerializable(typeof(ProjectConfig))]
// Response models
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(ContextShowResponse))]
[JsonSerializable(typeof(ContextStatusResponse))]
[JsonSerializable(typeof(GroundGetResponse))]
[JsonSerializable(typeof(GroundCreateResponse))]
[JsonSerializable(typeof(InitResponse))]
[JsonSerializable(typeof(DraftNoValidationResponse))]
[JsonSerializable(typeof(HistoryAddResponse))]
// GitHub CLI deserialization
[JsonSerializable(typeof(List<GhIssueListItem>))]
[JsonSerializable(typeof(GhIssueViewItem))]
internal partial class MillJsonContext : JsonSerializerContext;
