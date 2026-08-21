codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyChar: Char;
        MyTypeHelper: Codeunit "Type Helper";
    begin
        MyChar := MyTypeHelper.LFSeparator();
    end;
}