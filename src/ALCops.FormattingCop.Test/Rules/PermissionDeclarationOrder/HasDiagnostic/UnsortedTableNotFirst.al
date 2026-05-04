table 50100 "My Table"
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

permissionset 50100 "My Permission Set"
{
    Assignable = true;
    [|Permissions =
        codeunit "My Codeunit" = X,
        tabledata "My Table" = R|];
}
