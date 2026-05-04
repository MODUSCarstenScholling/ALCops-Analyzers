[||]table 50100 "My Table"
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}

codeunit 50100 "My Codeunit"
{
}

page 50100 "My Page"
{
}

permissionset 50100 "My Permission Set"
{
    Assignable = true;
    Permissions =
        table "My Table" = X,
        tabledata "My Table" = R,
        codeunit "My Codeunit" = X,
        page "My Page" = X;
}
