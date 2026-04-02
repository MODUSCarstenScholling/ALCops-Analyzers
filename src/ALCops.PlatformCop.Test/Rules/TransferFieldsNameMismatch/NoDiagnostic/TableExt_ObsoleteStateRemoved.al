tableextension 50100 MyCustomerExt extends Customer
{
    fields
    {
        [|field(50100; MyOtherField; Integer) { ObsoleteState = Removed; }|]
    }
}

tableextension 50101 MyContactExt extends Contact
{
    fields
    {
        [|field(50100; MyField; Integer) { }|]
    }
}

table 18 Customer { fields { field(1; "No."; Code[20]) { } } }
table 5050 Contact { fields { field(1; "No."; Code[20]) { } } }
