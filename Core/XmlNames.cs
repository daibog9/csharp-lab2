namespace Core;

public static class XmlNames {
    public const string Root = "StudentParliament";
    public const string Member = "Member";
    public const string FullName = "FullName";
    public const string LastName = "LastName";
    public const string FirstName = "FirstName";
    public const string Patronymic = "Patronymic";
    public const string Event = "Event";
    public const string Title = "Title";
    public const string Start = "Start";
    public const string End = "End";
    public const string Location = "Location";

    public const string University = "university";
    public const string AcademicYear = "academicYear";

    public const string Id = "id";
    public const string Faculty = "faculty";
    public const string Department = "department";
    public const string Specialty = "specialty";
    public const string Course = "course";
    public const string Group = "group";
    public const string Position = "position";
    public const string Email = "email";

    public const string Type = "type";
    public const string Format = "format";

    public static readonly IReadOnlySet<string> MainMemberAttributes =
        new HashSet<string> { Id, Faculty, Department, Specialty };

    public static string Label(string attributeName) => attributeName switch {
        Id => "Код",
        Faculty => "Факультет",
        Department => "Кафедра",
        Specialty => "Спеціальність",
        Course => "Курс",
        Group => "Група",
        Position => "Посада",
        Email => "Email",
        Type => "Вид заходу",
        Format => "Формат",
        _ => attributeName
    };
}