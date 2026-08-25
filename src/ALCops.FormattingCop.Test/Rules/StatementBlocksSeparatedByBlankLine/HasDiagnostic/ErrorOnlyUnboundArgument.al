codeunit 50128 MyErrorOnlyUnboundArgumentCodeunit
{
    procedure ErrorAfterStatement()
    begin
        Message('Something failed');
        [|Error|](UndefinedVar);
    end;
}
