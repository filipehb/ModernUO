using Server.Gumps;
using Server.Network;

namespace Server.Engines.BulkOrders
{
    public class SmallBODAcceptGump : Gump
    {
        private readonly SmallBOD m_Deed;
        private readonly Mobile m_From;

        public override bool Singleton => true;

        public SmallBODAcceptGump(Mobile from, SmallBOD deed) : base(50, 50)
        {
            m_From = from;
            m_Deed = deed;

            AddPage(0);

            AddBackground(25, 10, 430, 264, 5054);

            AddImageTiled(33, 20, 413, 245, 2624);
            AddAlphaRegion(33, 20, 413, 245);

            AddImage(20, 5, 10460);
            AddImage(430, 5, 10460);
            AddImage(20, 249, 10460);
            AddImage(430, 249, 10460);

            AddHtmlLocalized(190, 25, 120, 20, 1045133, 0x7FFF); // A bulk order
            AddHtmlLocalized(40, 48, 350, 20, 1045135, 0x7FFF);  // Ah!  Thanks for the goods!  Would you help me out?

            AddHtmlLocalized(40, 72, 210, 20, 1045138, 0x7FFF); // Amount to make:
            AddLabel(250, 72, 1152, deed.AmountMax.ToString());

            AddHtmlLocalized(40, 96, 120, 20, 1045136, 0x7FFF); // Item requested:
            AddItem(385, 96, deed.Graphic);
            AddHtmlLocalized(40, 120, 210, 20, deed.Number, 0x7FFF);

            if (deed.RequireExceptional || deed.Material != BulkMaterialType.None)
            {
                AddHtmlLocalized(40, 144, 210, 20, 1045140, 0x7FFF); // Special requirements to meet:

                if (deed.RequireExceptional)
                {
                    AddHtmlLocalized(40, 168, 350, 20, 1045141, 0x7FFF); // All items must be exceptional.
                }

                if (deed.Material != BulkMaterialType.None)
                {
                    AddHtmlLocalized(
                        40,
                        deed.RequireExceptional ? 192 : 168,
                        350,
                        20,
                        GetMaterialNumberFor(deed.Material), // All items must be made with x material.
                        0x7FFF
                    );
                }
            }

            AddHtmlLocalized(40, 216, 350, 20, 1045139, 0x7FFF); // Do you want to accept this order?

            AddButton(100, 240, 4005, 4007, 1);
            AddHtmlLocalized(135, 240, 120, 20, 1006044, 0x7FFF); // Ok

            AddButton(275, 240, 4005, 4007, 0);
            AddHtmlLocalized(310, 240, 120, 20, 1011012, 0x7FFF); // CANCEL
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (info.ButtonID == 1) // Ok
            {
                if (m_From.PlaceInBackpack(m_Deed))
                {
                    m_From.SendLocalizedMessage(1045152); // The bulk order deed has been placed in your backpack.
                }
                else
                {
                    m_From.SendLocalizedMessage(1045150); // There is not enough room in your backpack for the deed.
                    m_Deed.Delete();
                }
            }
            else
            {
                m_Deed.Delete();
            }
        }

        public override void OnServerClose(NetState owner)
        {
            if (m_Deed?.Deleted == false)
            {
                m_Deed.Delete();
            }
        }

        public static int GetMaterialNumberFor(BulkMaterialType material)
        {
            if (material >= BulkMaterialType.DullCopper && material <= BulkMaterialType.Valorite)
            {
                return 1045142 + (material - BulkMaterialType.DullCopper);
            }

            if (material >= BulkMaterialType.Spined && material <= BulkMaterialType.Barbed)
            {
                return 1049348 + (material - BulkMaterialType.Spined);
            }

            return 0;
        }
    }
}
