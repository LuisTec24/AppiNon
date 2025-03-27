using System;
using System.Collections.Generic;

namespace AppiNon.Models;

public partial class GomezTable
{
    public int Id { get; set; }

    public string Clave { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Ubicacion { get; set; } = null!;

    public string Ocupacion { get; set; } = null!;

    public string EsActivo { get; set; } = null!;
}
