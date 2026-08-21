codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyText: Text;
        TypeHelper: Codeunit "Type Helper";
    begin
        MyText := TypeHelper.CRLFSeparator();
    end;
}