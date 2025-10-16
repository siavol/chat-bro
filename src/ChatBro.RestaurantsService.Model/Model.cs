namespace ChatBro.RestaurantsService.Model;

public record RestaurantMenuItem(
    string Name,
    bool LactoseFree = false,
    bool GlutenFree = false);

public record Restaurant(
    string Name,
    List<RestaurantMenuItem> MenuItems,
    double Distance,
    List<string> Messages);
