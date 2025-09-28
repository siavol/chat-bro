namespace ChatBro.RestaurantsService;

public record RestaurantMenuItem(string Name, bool Laktositon = false, bool Maidoton = false, bool Gluuteniton = false);

public record Restaurant(string Name, List<RestaurantMenuItem> MenuItems, double Distance, List<string> Messages);
