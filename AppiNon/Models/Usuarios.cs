namespace AppiNon.Models
{
    public class Usuarios
    {
            public int id { get; set; }
            public string nombre { get; set; }
            public string correo { get; set; }
            public string contraseña_hash { get; set; }
            public int rol_id { get; set; }  

    }
}
