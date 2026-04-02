table 50100 MyTableA
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
    }
}

table 50101 MyTableB
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
    }
}

tableextension 50100 MyTableAExt extends MyTableA
{
    fields
    {
        [|field(50100; ExtField; Integer) { }|]
    }
}

tableextension 50101 MyTableBExt extends MyTableB
{
    fields
    {
        [|field(50100; ExtField; Boolean) { }|]
    }
}

codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        FromRec: Record MyTableB;
        ToRec: Record MyTableA;
    begin
        [|ToRec.TransferFields(FromRec)|];
    end;
}
