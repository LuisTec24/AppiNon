namespace AppiNon.Models
{
    public class Usuarios
    {
        public int ID { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Contraseña_hash { get; set; }

        public int Rol_id { get; set; }
     //   public Roles Rol { get; set; }

    }

}
