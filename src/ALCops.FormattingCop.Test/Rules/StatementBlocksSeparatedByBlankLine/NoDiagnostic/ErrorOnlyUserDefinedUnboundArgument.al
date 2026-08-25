codeunit 50129 MyErrorOnlyUserDefinedHandler
{
    procedure Error()
    begin
    end;
}

codeunit 50130 MyErrorOnlyUserDefinedCodeunit
{
    procedure ErrorAfterStatement()
    var
        Handler: Codeunit MyErrorOnlyUserDefinedHandler;
    begin
        Message('Something failed');
        [|Handler|].Error(UndefinedVar);
    end;
}
