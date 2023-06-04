using DynamicsFinal.ViewModels;

namespace DynamicsFinal; 

public static class DesignPreviewViewModels {
    public static MainViewModel MainViewModel { get; } = new() {State = new StateVector(1, 2, 0, 0)};
}