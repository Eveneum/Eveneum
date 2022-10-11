namespace Eveneum
{
    public enum DeleteMode
    {
        SoftDelete = 1,
        HardDelete = 2,
        TtlDelete = 3, // set time to live
    }
}
