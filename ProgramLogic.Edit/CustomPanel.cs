namespace ProgramLogic.Edit
{
	public class CustomPanel : System.Windows.Forms.Panel
	{
		protected override System.Drawing.Point ScrollToControl(System.Windows.Forms.Control activeControl)
		{
            // Возврат текущего местоположения не позволяет панели осущ. прокрутка к активному элементу управления при потере и восстановлении фокуса
            return this.DisplayRectangle.Location;
		}
	}
}
