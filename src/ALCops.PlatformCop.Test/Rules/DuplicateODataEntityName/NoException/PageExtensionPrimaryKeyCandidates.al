page 50100 MyPage
{
    PageType = List;
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            repeater(Lines)
            {
                field(Description; Rec.Description) { ApplicationArea = All; }
            }
        }
    }
}

pageextension 50101 MyPageExt extends MyPage
{
    layout
    {
        addlast(Lines)
        {
            field(AdditionalDescription; Rec.AdditionalDescription) { }
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Integer) { }
        field(2; Description; Text[100]) { }
        field(3; AdditionalDescription; Text[100]) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
