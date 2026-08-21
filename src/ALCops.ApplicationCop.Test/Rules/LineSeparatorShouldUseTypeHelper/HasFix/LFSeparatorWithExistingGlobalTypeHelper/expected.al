codeunit 50100 MyCodeunit
{
    var
        MyTypeHelper: Codeunit "Type Helper";

    procedure MyProcedure()
    var
        MyChar: Char;
    begin
        MyChar := MyTypeHelper.LFSeparator();
    end;
}